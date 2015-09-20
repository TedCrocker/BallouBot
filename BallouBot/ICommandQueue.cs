namespace BallouBot
{
	public interface ICommandQueue
	{
		void EnqueueCommand(string command);
		void EnqueueCommand(string command, QueuePriority priority);
		string DequeueCommand();
	}
}