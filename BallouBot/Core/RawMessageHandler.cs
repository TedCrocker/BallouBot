using System;
using System.Collections.Generic;
using BallouBot.Config;
using BallouBot.Interfaces;
using BallouBot.Logging;

namespace BallouBot.Core
{
	public static class RawMessageHandler
	{
		public async static void ProcessRawMessage(string rawMessage)
		{
			var parsedMessage = MessageParser.ParseIrcMessage(rawMessage);
			var chatParsers = PluginStore.Container.GetExports<IChatParser>();
			var logger = PluginStore.Container.GetExport<ILog>().Value;
			logger.Info(rawMessage);

			var userManager = new UserManager();
			await userManager.UpdateOrCreateUser(parsedMessage);

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
	}
}