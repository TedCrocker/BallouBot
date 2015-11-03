using System;
using System.Collections.Concurrent;
using BallouBot.Interfaces;
using BallouBot.Logging;

namespace BallouBot.Core
{
	public static class RawMessageHandler
	{
		internal static ConcurrentBag<ChatParserContainer> Parsers = new ConcurrentBag<ChatParserContainer>();

		public async static void ProcessRawMessage(string rawMessage)
		{
			var parsedMessage = MessageParser.ParseIrcMessage(rawMessage);
			var logger = PluginStore.Container.GetExport<ILog>().Value;

			logger.Info(rawMessage);

			var userManager = new UserManager();
			await userManager.UpdateOrCreateUser(parsedMessage);

			foreach (var chatParser in Parsers)
			{
				if (chatParser.IsEnabled)
				{
					try
					{
						await chatParser.Parser.ReceiveMessage(parsedMessage);
					}
					catch (Exception e)
					{
						logger.Error(e, $"\r\n{rawMessage}", rawMessage);
					}
				}
			}
		}
	}
}