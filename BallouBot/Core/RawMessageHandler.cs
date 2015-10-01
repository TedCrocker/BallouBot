using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
			if (_useresToIgnore.Contains(parsedMessage.User))
			{
				return;
			}
			if (parsedMessage.Command == Constants.UserStateCommand)
			{
				var dataStore = PluginStore.Container.GetExport<IDataSource>().Value;
				var user = dataStore.Repository<User>().Get(parsedMessage.User);

				if (user == null)
				{
					user = new User()
					{
						Id = parsedMessage.User,
						Channels = new ConcurrentDictionary<string, UserChannel>(),
						Data = new ConcurrentDictionary<string, object>()
					};

					var api = new TwitchApi();
					try
					{
						api.SetUserInfo(user);
						dataStore.Repository<User>().Create(user);
					}
					catch (Exception e)
					{
						logger.Error(e);
					}
				}

				if (!user.Channels.ContainsKey(parsedMessage.Channel))
				{
					user.Channels.Add(parsedMessage.Channel, new UserChannel()
					{
						Name = parsedMessage.Channel
					});
				}
				if (parsedMessage.Tags.ContainsKey("user-type") && parsedMessage.Tags["user-type"] == "mod")
				{
					user.Channels[parsedMessage.Channel].IsModerator = true;
				}
			}
		}
	}
}