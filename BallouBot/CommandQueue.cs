using System.Collections.Concurrent;

namespace BallouBot
{
	public class CommandQueue : ICommandQueue
	{
		private readonly ConcurrentQueue<string> _highPriorityQueue;
		private readonly ConcurrentQueue<string> _normalPriorityQueue;
		private readonly ConcurrentQueue<string> _lowPriorityQueue;

		public CommandQueue()
		{
			_lowPriorityQueue = new ConcurrentQueue<string>();
			_normalPriorityQueue = new ConcurrentQueue<string>();
			_highPriorityQueue = new ConcurrentQueue<string>();
		}

		public void EnqueueCommand(string command)
		{
			EnqueueCommand(command, QueuePriority.Normal);
		}

		public void EnqueueCommand(string command, QueuePriority priority)
		{
			switch (priority)
			{
				case QueuePriority.Low:
					_lowPriorityQueue.Enqueue(command);
					break;
				case QueuePriority.High:
					_highPriorityQueue.Enqueue(command);
					break;
				default:
					_normalPriorityQueue.Enqueue(command);
					break;
			}
		}

		public string DequeueCommand()
		{
			string command = "";
			if (!_highPriorityQueue.IsEmpty)
			{
				_highPriorityQueue.TryDequeue(out command);
			}
			else if (!_normalPriorityQueue.IsEmpty)
			{
				_normalPriorityQueue.TryDequeue(out command);
			}
			else if (!_lowPriorityQueue.IsEmpty)
			{
				_lowPriorityQueue.TryDequeue(out command);
			}

			return command;
		}
	}
}