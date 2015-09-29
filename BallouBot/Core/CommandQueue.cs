using System;
using System.Collections.Concurrent;
using BallouBot.Interfaces;

namespace BallouBot.Core
{
	public class CommandQueue : ICommandQueue
	{
		private readonly ConcurrentQueue<string> _highPriorityQueue;
		private readonly ConcurrentQueue<string> _normalPriorityQueue;
		private readonly ConcurrentQueue<string> _lowPriorityQueue;

		private readonly ConcurrentQueue<DateTime> _processedCommands;

		public CommandQueue()
		{
			_lowPriorityQueue = new ConcurrentQueue<string>();
			_normalPriorityQueue = new ConcurrentQueue<string>();
			_highPriorityQueue = new ConcurrentQueue<string>();
			_processedCommands = new ConcurrentQueue<DateTime>();
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

		private void ClearQueue()
		{
			if (!_processedCommands.IsEmpty)
			{
				var now = DateTime.Now;
				DateTime toCompare;
				_processedCommands.TryPeek(out toCompare);

				if ((now - toCompare).TotalSeconds > Constants.CommandDequeueTimeLimit)
				{
					_processedCommands.TryDequeue(out toCompare);
				}
			}
		}

		public string DequeueCommand()
		{
			string command = "";
			ClearQueue();

			if (_processedCommands.Count < Constants.CommandDequeueCommandLimit)
			{
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

				if (!string.IsNullOrWhiteSpace(command))
				{
					_processedCommands.Enqueue(DateTime.Now);
				}
			}

			return command;
		}
	}
}