using System.Collections.Generic;
using BallouBot;
using BallouBot.ChatParsers;
using BallouBotTests.Mocks;
using Xunit;

namespace BallouBotTests
{
	public class UptimeHandlerTests
	{
		[Fact]
		public async void CanGetUptime()
		{
			var commandQueue = new MockCommandQueue();
			var twitchApi = new MockTwitchApi();
			var uptimeHandler = new UptimeHandler(commandQueue, twitchApi);
			var message = new Message()
			{
				Command = Constants.PrivateMessageCommand,
				Suffix = "!uptime",
				Parameters = new List<string> { "#testChannel"}
			};

			await uptimeHandler.ReceiveMessage(message);

			var uptime = commandQueue.DequeueCommand();
			Assert.NotNull(uptime);
			Assert.NotEqual(uptime, "");
		}

		[Fact]
		public async void UptimeCannotBeSpammed()
		{
			var commandQueue = new MockCommandQueue();
			var twitchApi = new MockTwitchApi();
			var uptimeHandler = new UptimeHandler(commandQueue, twitchApi);
			var message = new Message()
			{
				Command = Constants.PrivateMessageCommand,
				Suffix = "!uptime",
				Parameters = new List<string> { "#testChannel" }
			};

			await uptimeHandler.ReceiveMessage(message);
			await uptimeHandler.ReceiveMessage(message);

			var uptime1 = commandQueue.DequeueCommand();
			var uptime2 = commandQueue.DequeueCommand();
			Assert.NotEqual(uptime1, "");
			Assert.Equal(uptime2, "");
		}
	}
}