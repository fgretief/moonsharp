using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MoonSharp.Interpreter
{
	public class ScriptTask : TaskCompletionSource<DynValue>
	{
		public Coroutine Coroutine;
		public DynValue[] Args;

		public static ScriptTask Create(Script script, DynValue func, params DynValue[] args)
		{
			if (script == null)
				throw new ArgumentNullException("script");
			if (func == null)
				throw new ArgumentNullException("func");
			Contract.Requires(func.Type == DataType.Function || func.Type == DataType.ClrFunction);
			Contract.EndContractBlock();

			return new ScriptTask()
			{
				Coroutine = script.CreateCoroutine(func).Coroutine,
				Args = args
			};
		}

		private ScriptTask()
		{
			/* Use Create() method for construction */
		}

		private static void ContinueLuaScript(Task t, object o)
		{
			var scriptTask = (ScriptTask)o;
			scriptTask.Args = null;

			/* If task is a generic Task<T>,
			 * use Task<T>.Result as input for coroutine.resume() function */
			var tt = t.GetType();
			if (tt.IsGenericType)
			{
				var pi = tt.GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
				object result = pi.GetValue(t);

				try // FIXME: is there a beter way to do this? TryFromObject(...)
				{
					scriptTask.Args = new[]
					{
						DynValue.FromObject(scriptTask.Coroutine.OwnerScript, result)
					};
				}
				catch (ScriptRuntimeException)
				{ }
			}

			ResumeLuaScript(scriptTask);
		}

		internal static void ResumeLuaScript(object o)
		{
			ResumeLuaScript((ScriptTask)o);
		}

		public static void ResumeLuaScript(ScriptTask st)
		{
			Debug.Assert(SynchronizationContext.Current is ScriptSynchronizationContext, "You are not on the script thread!");
			try
			{
				var result = st.Coroutine.Resume(st.Args ?? new DynValue[0]);

				if (st.Coroutine.State == CoroutineState.Dead)
				{
					st.SetResult(result);
				}
				else if (st.Coroutine.State == CoroutineState.ForceSuspended)
				{
					// TODO: what should we do if we get a forced suspend? For now, treat it as a yield.
					st.Args = null;
					System.Threading.Tasks.Task.Factory.StartNew(ResumeLuaScript, st,
						CancellationToken.None, TaskCreationOptions.None,
						TaskScheduler.FromCurrentSynchronizationContext());
				}
				else if (st.Coroutine.State == CoroutineState.Suspended)
				{
					var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

					Task task;
					if (result.Type == DataType.UserData &&
						(task = result.UserData.Object as Task) != null)
					{
						// Special yield, resume the Lua task on script thread
						task.ContinueWith(ContinueLuaScript, st, scheduler);
					}
					else
					{
						var f = result.ToScalar();
						if (f.Type == DataType.Function)
						{
							DynValue[] a = null;
							if (result.Type == DataType.Tuple && result.Tuple.Length > 0)
								a = result.Tuple.Skip(1).ToArray();

							var script = st.Coroutine.OwnerScript;

							// Start a new parallel Lua task on the script thread
							task = System.Threading.Tasks.Task.Factory.StartNew(ResumeLuaScript,
								Create(script, f, a),
								CancellationToken.None,
								TaskCreationOptions.None,
								scheduler);

							result = DynValue.FromObject(script, task);
						}

						// Normal yield, resume the current Lua task on script thread
						st.Args = new []{ result };
						System.Threading.Tasks.Task.Factory.StartNew(ResumeLuaScript, st,
							CancellationToken.None, TaskCreationOptions.None, scheduler);
					}
				}
			}
			catch (Exception ex)
			{
				st.SetException(ex);
			}
		}

		internal void ResumeScript(ScriptSynchronizationContext context)
		{
			System.Threading.Tasks.Task.Factory.StartNew(ResumeLuaScript, this,
				CancellationToken.None, TaskCreationOptions.None, context.ScriptScheduler);
		}
	}
}