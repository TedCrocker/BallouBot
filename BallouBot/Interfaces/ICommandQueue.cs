using System;
using System.Threading;

namespace BallouBot.Interfaces
{
	public interface ICommandQueue
	{
		event EventHandler CommandQueued;
		void EnqueueCommand(string command);
		void EnqueueCommand(string command, QueuePriority priority);
		string DequeueCommand();
	}
}