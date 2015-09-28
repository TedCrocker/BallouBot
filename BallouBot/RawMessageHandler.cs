using System;
using System.Collections.Concurrent;
using BallouBot.Data;
using BallouBot.Twitch;

namespace BallouBot
{
	public static class RawMessageHandler
	{
		public async static void ProcessRawMessage(string rawMessage)
		{
			var parsedMessage = MessageParser.ParseIrcMessage(rawMessage);
			var chatParsers = PluginStore.Container.GetExports<IChatParser>();

			MakeSureUserIsBuilt(parsedMessage);

			foreach (var chatParser in chatParsers)
			{
				try
				{
					await chatParser.Value.ReceiveMessage(parsedMessage);
				}
				catch (Exception e)
				{
					
				}
			}
		}

		private async static void MakeSureUserIsBuilt(Message parsedMessage)
		{
			var dataStore = PluginStore.Container.GetExport<IDataSource>().Value;
			var user = dataStore.Repository<User>().Get(parsedMessage.User);
			if (user == null)
			{
				var userObject = new User()
				{
					Id = parsedMessage.User,
					Data = new ConcurrentDictionary<string, object>()
				};

				var api = new TwitchApi();
				try
				{
					await api.SetUserInfo(userObject);
					await dataStore.Repository<User>().Create(userObject);
				}
				catch (Exception e){}
			}

		}
	}
}