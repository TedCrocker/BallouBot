using System.Threading;
using BallouBot.Interfaces;
using IrcDotNet;

namespace BallouBot.Core
{
	public class EventLoop
	{
		public EventLoop(AutoResetEvent resetEvent)
		{
			_resetEvent = resetEvent;
		}

		private volatile bool stopRunning = false;
		private readonly AutoResetEvent _resetEvent;

		public void Start(TwitchIrcClient client, ICommandQueue queue)
		{
			while (!stopRunning)
			{
				var command = queue.DequeueCommand();

				if (string.IsNullOrWhiteSpace(command))
				{
					_resetEvent.WaitOne();
				}
				if (command != "exit")
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