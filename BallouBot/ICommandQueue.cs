namespace BallouBot
{
	public interface ICommandQueue
	{
		void EnqueueCommand(string command);
		string DequeueCommand();
	}
}