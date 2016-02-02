using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MoonSharp.Interpreter
{
	public class AsyncScript : Script
	{
		public ScriptSynchronizationContext Context { get; private set; }
		public readonly Thread Thread;

		public AsyncScript()
			: this(CoreModules.Preset_Default)
		{}

		public AsyncScript(CoreModules coreModules)
			: base(coreModules)
		{
			UserData.RegisterType<ScriptSynchronizationContext>();
			UserData.RegisterType<System.Threading.Tasks.Task>();

			Context = new ScriptSynchronizationContext();

			Globals["sleep"] = DynValue.NewCallback(SleepAsync);

			Thread = new Thread(Context.RunOnCurrentThread);
			Thread.Name = "Lua Script Thread";
			Thread.IsBackground = true;
			Thread.Start();
		}

		private DynValue SleepAsync(ScriptExecutionContext context, CallbackArguments args)
		{
			var script = context.GetScript();
			var milliseconds = args.AsInt(0, "sleep");
			// Lua: coroutine.yield( Task.Delay( milliseconds ) )
			return DynValue.NewYieldReq(new[]
			{
				DynValue.FromObject(script, Task.Delay(milliseconds))
			});
		}

		public Task<DynValue> CallAsync(DynValue function, params DynValue[] args)
		{
			var scriptTask = ScriptTask.Create(this, function, args);
			scriptTask.ResumeScript(Context);
			return scriptTask.Task;
		}
	}
}