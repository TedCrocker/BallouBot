using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using BallouBot.ChatParsers;
using BallouBot.Core;
using BallouBot.Data;
using BallouBotTests.Mocks;
using Xunit;

namespace BallouBotTests
{
	public class LinkSpamHandlerTests
	{
		[Fact]
		public async void ShouldTimeoutLinkSpamMessages()
		{
			var commandQueue = new MockCommandQueue();
			var dataSource = new MockDataSource();
			var linkSpamHandler = new LinkSpamFilter(commandQueue, dataSource);
			var message = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :Check it out! www.google.com");
			await linkSpamHandler.ReceiveMessage(message);

			var command = commandQueue.DequeueCommand();
			Assert.NotEqual(command, "");
		}

		[Fact]
		public async void ShouldLetAdminsPostLinks()
		{
			var commandQueue = new MockCommandQueue();
			var dataSource = new MockDataSource();
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

			var linkSpamHandler = new LinkSpamFilter(commandQueue, dataSource);
			var message = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :Check it out! www.google.com");
			await linkSpamHandler.ReceiveMessage(message);

			var command = commandQueue.DequeueCommand();
			Assert.Equal(command, "");
		}

		[Fact]
		public async void AdminCanGratPermissionToUserForLinks()
		{
			var commandQueue = new MockCommandQueue();
			var dataSource = new MockDataSource();
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
				},
				Data = new Dictionary<string, object>()
			});
			repo.ObjectCache.Add("taylormade86", new User()
			{
				Channels = new Dictionary<string, UserChannel>()
				{
					{
						"#ballouthebear", new UserChannel()
						{
							IsModerator = false
						}
					}
				},
				Data = new Dictionary<string, object>()
			});

			var linkSpamHandler = new LinkSpamFilter(commandQueue, dataSource);
			var adminMessage = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :!permit taylormade86");
			await linkSpamHandler.ReceiveMessage(adminMessage);
			var command = commandQueue.DequeueCommand();
			Assert.Equal(command, "PRIVMSG #ballouthebear :User taylormade86 has been permitted to post a link.");

			var userMessage = MessageParser.ParseIrcMessage("@color=#FF0000;display-name=Taylormade86;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :taylormade86!taylormade86@taylormade86.tmi.twitch.tv PRIVMSG #ballouthebear :Check it out! www.google.com");
			await linkSpamHandler.ReceiveMessage(userMessage);

			command = commandQueue.DequeueCommand();
			Assert.Equal(command, "");
		}
	}
}