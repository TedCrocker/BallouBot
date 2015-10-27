using System.Collections.Generic;
using BallouBot;
using BallouBot.Interfaces;

namespace BallouBotTests.Mocks
{
	public class MockCommandQueue : ICommandQueue
	{
		private readonly Queue<string> _queue;

		public MockCommandQueue()
		{
			_queue = new Queue<string>();
		}

		public void EnqueueCommand(string command)
		{
			_queue.Enqueue(command);
		}

		public string DequeueCommand()
		{
			if (_queue.Count > 0)
			{
				return _queue.Dequeue();
			}

			return "";
		}

		public void EnqueueCommand(string command, QueuePriority priority)
		{
			_queue.Enqueue(command);
		}
	}
}