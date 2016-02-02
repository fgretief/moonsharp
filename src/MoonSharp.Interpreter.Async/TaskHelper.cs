using System;
using System.Threading.Tasks;

namespace MoonSharp.Interpreter
{
	public static class TaskHelper
	{
		private static readonly Lazy<Task> _completedTask = new Lazy<Task>(() => Task.FromResult(0));

		private static readonly Lazy<Task> _canceledTask = new Lazy<Task>(() =>
		{
			var tcs = new TaskCompletionSource<bool>();
			tcs.SetCanceled();
			return tcs.Task;
		});

		/// <summary>
		/// Returns a completed task
		/// </summary>
		public static Task CompletedTask
		{
			get { return _completedTask.Value; }
		}

		/// <summary>
		/// Return a canceled task
		/// </summary>
		public static Task CanceledTask
		{
			get { return _canceledTask.Value; }
		}

		/// <summary>
		/// Returns a faulted task with the provided exception
		/// </summary>
		public static Task FromException(Exception exception)
		{
			var tcs = new TaskCompletionSource<object>();
			tcs.SetException(exception);
			return tcs.Task;
		}

		/// <summary>
		/// Returns a faulted task with the provided exception
		/// </summary>
		public static Task<TResult> FromException<TResult>(Exception exception)
		{
			var tcs = new TaskCompletionSource<TResult>();
			tcs.SetException(exception);
			return tcs.Task;
		}
	}

	public static class TaskHelper<T>
	{
		private static readonly Lazy<Task<T>> _completedTask = new Lazy<Task<T>>(() => Task.FromResult(default(T)));

		private static readonly Lazy<Task<T>> _canceledTask = new Lazy<Task<T>>(() =>
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetCanceled();
			return tcs.Task;
		});

		/// <summary>
		/// Returns a completed task
		/// </summary>
		public static Task<T> CompletedTask
		{
			get { return _completedTask.Value; }
		}

		/// <summary>
		/// Return a canceled task
		/// </summary>
		public static Task<T> CanceledTask
		{
			get { return _canceledTask.Value; }
		}

		/// <summary>
		/// Returns a faulted task with the provided exception
		/// </summary>
		public static Task<T> FromException(Exception exception)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetException(exception);
			return tcs.Task;
		}
	}
}