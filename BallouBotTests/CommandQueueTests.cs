using System.Runtime.InteropServices.ComTypes;
using BallouBot;
using BallouBot.Core;
using Xunit;

namespace BallouBotTests
{
	public class CommandQueueTests
	{
		[Fact]
		public void CanOnlyPullACertainNumberOfCommands()
		{
			var commandQueue = new CommandQueue();
			for (var i = 0; i < 100; i++)
			{
				commandQueue.EnqueueCommand("command");
			}

			var dequeuedCommands = 0;
			for (var i = 0; i < 100; i++)
			{
				var command = commandQueue.DequeueCommand();
				if (command == "command")
				{
					dequeuedCommands++;
				}
			}

			Assert.Equal(dequeuedCommands, Constants.CommandDequeueCommandLimit);
		}
	}
}