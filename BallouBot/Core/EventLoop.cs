using BallouBot.Interfaces;
using IrcDotNet;

namespace BallouBot.Core
{
	public class EventLoop
	{
		private volatile bool stopRunning = false;

		public void Start(TwitchIrcClient client, ICommandQueue queue)
		{
			while (!stopRunning)
			{
				var command = queue.DequeueCommand();

				if (command != "exit" && !string.IsNullOrWhiteSpace(command))
				{
					client.SendRawMessage(command);
				}
				else if (command == "exit")
				{
					stopRunning = true;
				}
			}	
		}

		public void Stop()
		{
			stopRunning = true;
		}
	}
}