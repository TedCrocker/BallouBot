using System;
using IrcDotNet;

namespace BallouBot
{
	public static class ConnectionEvents
	{
		public static void OnRegistered(object sender, EventArgs e){}

		public async static void RawMessageReceived(object sender, IrcRawMessageEventArgs args)
		{
			if (!args.RawContent.StartsWith("PING"))
			{
				RawMessageHandler.ProcessRawMessage(args.RawContent);
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