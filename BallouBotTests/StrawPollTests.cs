using System.Collections.Generic;
using BallouBot.Core;
using BallouBot.Data;
using BallouBot.PollPlugin;
using BallouBotTests.Mocks;
using Xunit;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Registration;
using System.Linq;
using BallouBot.Interfaces;

namespace BallouBotTests
{
	public class StrawPollTests
	{
		[Fact]
		public async void CanCreatePoll()
		{
			var poll = new StrawPoll();
			var id = await poll.Create("Higgildy", new List<string> {"piggildy", "poo"});

			Assert.NotSame("", id);
		}

		[Fact]
		public async void CanFetchPoll()
		{
			var poll = new StrawPoll();
			var id = await poll.Create("Higgildy", new List<string> { "piggildy", "poo" });

			Assert.NotSame("", id);

			var pollModel = await poll.Fetch(id.Split('/').Last());
			Assert.NotNull(pollModel);
		}

		[Fact]
		public void CanParsePollMessage()
		{
			var pollUserRequest = "!poll \"Do you like big butts?\";\"Yes\";\"No\"";
			var result = PollHelpers.MapStringToPostModel(pollUserRequest);

			Assert.Equal(result.Item1, "Do you like big butts?");
			Assert.Equal(result.Item2.Count, 2);
		}

		[Fact]
		public async void PollHandlerRejectsOnlyOneOption()
		{
			var commandQ = new MockCommandQueue();
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
			var rawMessage = "@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :!poll \"Do you like big butts?\";\"Yes\"";
			var message = MessageParser.ParseIrcMessage(rawMessage);
			var pollHandler = new PollHandler(commandQ, dataSource);

			await pollHandler.ReceiveMessage(message);
			var result = commandQ.DequeueCommand();

			Assert.NotNull(result);
			Assert.NotEqual("", result);
			Assert.Contains("You must have at least two options!", result);
		}

		[Fact]
		public async void PollHandlerCanCreateAndPostPoll()
		{
			PluginStore.InitializePluginStoreNew(builder => new AssemblyCatalog(GetType().Assembly, builder));
			var commandQ = new MockCommandQueue();
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
			var rawMessage = "@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :!poll \"Do you like big butts?\";\"Yes\";\"No\"";
			var message = MessageParser.ParseIrcMessage(rawMessage);
			var pollHandler = new PollHandler(commandQ, dataSource);

			await pollHandler.ReceiveMessage(message);
			var result = commandQ.DequeueCommand();

			Assert.NotNull(result);
			Assert.NotEqual("", result);
			Assert.Contains("Do you like big butts?", result);
		}

		[Fact]
		public async void MustWaitBetweenPollCreations()
		{
			PluginStore.InitializePluginStoreNew(builder => new AssemblyCatalog(GetType().Assembly, builder));
			var commandQ = new MockCommandQueue();
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
			var rawMessage = "@color=#FF0000;display-name=BallouTheBear;emotes=;subscriber=0;turbo=0;user-id=30514348;user-type= :ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :!poll \"Do you like big butts?\";\"Yes\";\"No\"";
			var message = MessageParser.ParseIrcMessage(rawMessage);
			var pollHandler = new PollHandler(commandQ, dataSource);

			await pollHandler.ReceiveMessage(message);
			await pollHandler.ReceiveMessage(message);
			var command1 = commandQ.DequeueCommand();
			var command2 = commandQ.DequeueCommand();

			Assert.NotEqual("", command2);
			Assert.Contains("You must wait ", command2);
			Assert.Contains(" minutes before you can create another poll.", command2);
		}
	}

	internal class MockStrawPollPluginRegister : IPluginRegister
	{
		public IList<AssemblyCatalog> Register(RegistrationBuilder builder)
		{
			builder.ForType<MockPoll>().Export<IPoll>();

			var catalogs = new List<AssemblyCatalog>();

			catalogs.Add(new AssemblyCatalog(typeof(IPoll).Assembly, builder));
			catalogs.Add(new AssemblyCatalog(typeof(MockPoll).Assembly, builder));

			return catalogs;
		}
	}
}