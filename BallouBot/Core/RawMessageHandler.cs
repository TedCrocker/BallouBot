using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BallouBot.Config;
using BallouBot.Data;
using BallouBot.Interfaces;
using BallouBot.Logging;
using BallouBot.Twitch;

namespace BallouBot.Core
{
	public static class RawMessageHandler
	{
		public async static void ProcessRawMessage(string rawMessage)
		{
			var parsedMessage = MessageParser.ParseIrcMessage(rawMessage);
			var chatParsers = PluginStore.Container.GetExports<IChatParser>();
			var logger = PluginStore.Container.GetExport<ILog>().Value;

			MakeSureUserIsBuilt(parsedMessage, logger);

			foreach (var chatParser in chatParsers)
			{
				try
				{
					await chatParser.Value.ReceiveMessage(parsedMessage);
				}
				catch (Exception e)
				{
					logger.Error(e, $"\r\n{rawMessage}", rawMessage);
				}
			}
		}

		private static IList<string> _useresToIgnore = new List<string>()
		{
			"jtv",
            Constants.TwitchUser
        };


		private static async void MakeSureUserIsBuilt(Message parsedMessage, ILog logger)
		{
			if (parsedMessage.Command == Constants.UserStateCommand && !_useresToIgnore.Contains(parsedMessage.User))
			{
				await HandleUserStateCommand(parsedMessage, logger);
			}
			if (parsedMessage.Command == Constants.ModeCommand)
			{
				await HandleModeCommand(parsedMessage, logger);
			}
		}

		private static async Task HandleUserStateCommand(Message parsedMessage, ILog logger)
		{
			var dataStore = PluginStore.Container.GetExport<IDataSource>().Value;
			var repository = dataStore.Repository<User>();
			var user = await repository.Get(parsedMessage.User);

			if (user == null)
			{
				user = await CreateUser(parsedMessage.User, logger, repository);
			}

			await CreateChannelForUser(parsedMessage, user, repository);
			if (parsedMessage.Tags.ContainsKey("user-type") && parsedMessage.Tags["user-type"] == "mod")
			{
				user.Channels[parsedMessage.Channel].IsModerator = true;
				await repository.Update(user.Id, user);
			}
		}

		private static async Task CreateChannelForUser(Message parsedMessage, User user, IRepository<User> repository)
		{
			if (!user.Channels.ContainsKey(parsedMessage.Channel))
			{
				user.Channels.Add(parsedMessage.Channel, new UserChannel()
				{
					Name = parsedMessage.Channel
				});
				await repository.Update(user.Id, user);
			}
		}

		private static async Task<User> CreateUser(string userID, ILog logger, IRepository<User> repository)
		{
			User user = new User()
			{
				Id = userID,
				Channels = new ConcurrentDictionary<string, UserChannel>(),
				Data = new ConcurrentDictionary<string, object>()
			};

			var api = PluginStore.Container.GetExport<ITwitchApi>().Value;
			try
			{
				await api.SetUserInfo(user);
				await repository.Create(user);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}

			return user;
		}

		private static async Task HandleModeCommand(Message parsedMessage, ILog logger)
		{
			if (parsedMessage.RawMessage.Contains("+o"))
			{
				var dataStore = PluginStore.Container.GetExport<IDataSource>().Value;
				var repository = dataStore.Repository<User>();
				var userID = parsedMessage.RawMessage.Trim().Split(' ').Last();
				var user = await repository.Get(userID);
				if (user == null)
				{
					user = await CreateUser(userID, logger, repository);
				}
				await CreateChannelForUser(parsedMessage, user, repository);
				user.Channels[parsedMessage.Channel].IsModerator = true;
			}
		}
	}
}