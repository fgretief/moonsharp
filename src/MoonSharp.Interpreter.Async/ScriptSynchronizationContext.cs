using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MoonSharp.Interpreter
{
	public class ScriptSynchronizationContext : SynchronizationContext
	{
		public TaskScheduler ScriptScheduler { get; private set; }

		readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> _queue =
			 new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

		public override void Post(SendOrPostCallback d, object state)
		{
			_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
		}

		public void RunOnCurrentThread()
		{
			SetSynchronizationContext(this);

			ScriptScheduler = TaskScheduler.FromCurrentSynchronizationContext();

			KeyValuePair<SendOrPostCallback, object> workItem;

			while (_queue.TryTake(out workItem, Timeout.Infinite))
			{
				Send(workItem.Key, workItem.Value);
			}
		}
	}
}
