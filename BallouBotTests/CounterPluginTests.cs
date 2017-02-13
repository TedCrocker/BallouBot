using System.Collections.Generic;
using BallouBot.Core;
using BallouBot.CounterPlugin;
using BallouBot.Data;
using BallouBotTests.Mocks;
using Xunit;

namespace BallouBotTests
{
	public class CounterPluginTests
	{
		private readonly MockCommandQueue _mockCommandQueue;
		private readonly MockDataSource _mockDataSource;
		private readonly CounterHandler _counterHandler;
		private string _createMessage = "@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :!counter create $death";

		public CounterPluginTests()
		{
			_mockCommandQueue = new MockCommandQueue();
			_mockDataSource = new MockDataSource();
			AddModerator(_mockDataSource);
			_counterHandler = new CounterHandler(_mockCommandQueue, _mockDataSource);

		}

		[Fact]
		public async void CanCreateCounter()
		{
			var message = MessageParser.ParseIrcMessage(_createMessage);
			await _counterHandler.ReceiveMessage(message);
			var response = _mockCommandQueue.DequeueCommand();

			Assert.NotNull(response);
			Assert.Contains("Counter $death created", response);
		}

		[Fact]
		public async void CanCreateCounterWithCustomMessage()
		{
			var message = MessageParser.ParseIrcMessage(_createMessage + " Died # times");
			await _counterHandler.ReceiveMessage(message);
			_mockCommandQueue.DequeueCommand();

			var incrementMessage = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :$death");
			await _counterHandler.ReceiveMessage(incrementMessage);
			var response = _mockCommandQueue.DequeueCommand();

			Assert.NotNull(response);
			Assert.Contains("Died 1 times", response);
		}

		[Fact]
		public async void CanDeleteCounter()
		{
			var createMessage = MessageParser.ParseIrcMessage(_createMessage);
			await _counterHandler.ReceiveMessage(createMessage);
			_mockCommandQueue.DequeueCommand();
			var deleteMessage = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :!counter delete $death");
			await _counterHandler.ReceiveMessage(deleteMessage);
			var response = _mockCommandQueue.DequeueCommand();

			Assert.NotEmpty(response);
			Assert.Contains("Counter $death removed", response);
		}

		[Fact]
		public async void CanResetCounter()
		{
			var createMessage = MessageParser.ParseIrcMessage(_createMessage);
			await _counterHandler.ReceiveMessage(createMessage);
			_mockCommandQueue.DequeueCommand();
			var resetMessage = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :!counter reset $death");
			await _counterHandler.ReceiveMessage(resetMessage);
			var response = _mockCommandQueue.DequeueCommand();
			Assert.NotEmpty(response);
			Assert.Contains("Counter $death reset", response);
		}

		[Fact]
		public async void CanIncrementCounter()
		{
			var createMessage = MessageParser.ParseIrcMessage(_createMessage);
			await _counterHandler.ReceiveMessage(createMessage);
			_mockCommandQueue.DequeueCommand();
			var incrementMessage = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :$death");
			await _counterHandler.ReceiveMessage(incrementMessage);
			var response = _mockCommandQueue.DequeueCommand();
			Assert.NotEmpty(response);
			Assert.Contains("death 1 times", response);
		}

		[Fact]
		public async void CanIncrementBySpecifiedAmount()
		{
			var createMessage = MessageParser.ParseIrcMessage(_createMessage);
			await _counterHandler.ReceiveMessage(createMessage);
			_mockCommandQueue.DequeueCommand();
			var incrementMessage = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :$death+16");
			await _counterHandler.ReceiveMessage(incrementMessage);
			var response = _mockCommandQueue.DequeueCommand();
			Assert.NotEmpty(response);
			Assert.Contains("death 16 times", response);
		}

		[Fact]
		public async void CanDecrementCounter()
		{
			var createMessage = MessageParser.ParseIrcMessage(_createMessage);
			await _counterHandler.ReceiveMessage(createMessage);
			_mockCommandQueue.DequeueCommand();
			var decrement = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :$death-");
			await _counterHandler.ReceiveMessage(decrement);
			var response = _mockCommandQueue.DequeueCommand();
			Assert.NotEmpty(response);
			Assert.Contains("death -1 times", response);
		}

		[Fact]
		public async void CanDecrementBySpecifiedAmount()
		{
			var createMessage = MessageParser.ParseIrcMessage(_createMessage);
			await _counterHandler.ReceiveMessage(createMessage);
			_mockCommandQueue.DequeueCommand();
			var decrement = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :$death-5");
			await _counterHandler.ReceiveMessage(decrement);
			var response = _mockCommandQueue.DequeueCommand();
			Assert.NotEmpty(response);
			Assert.Contains("death -5 times", response);
		}

		private void AddModerator(IDataSource dataSource)
		{
			var repo = dataSource.Repository<User>() as MockRepository<User>;
			repo.ObjectCache.Add("ballouthebear", new User()
			{
				Channels = new Dictionary<string, UserChannel>()
				{
					{
						"#ballouthebear", new UserChannel()
						{
							IsModerator = true
						}
					}

				}
			});
		}
	}
}