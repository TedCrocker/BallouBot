﻿using System;
using IrcDotNet;

namespace BallouBot
{
	public static class ConnectionEvents
	{
		public static void OnRegistered(object sender, EventArgs e){}

		public static void RawMessageReceived(object sender, IrcRawMessageEventArgs args)
		{
			if (!args.RawContent.StartsWith("PING"))
			{
				var parsedMessage = MessageParser.ParseIrcMessage(args.RawContent);

				var chatParsers = PluginStore.Container.GetExports<IChatParser>();
				foreach (var chatParser in chatParsers)
				{
					chatParser.Value.ReceiveMessage(parsedMessage);
				}
			}
		}

		public static void OnDisconnected(object sender, EventArgs e)
		{
			Console.WriteLine("Disconnected");
		}

		public static void OnConnected(object sender, EventArgs e)
		{
			Console.WriteLine("Connected");
		}

		public static void OnConnectFailed(object sender, IrcErrorEventArgs e)
		{
			throw new NotImplementedException();
		}
	}
}