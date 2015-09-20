using System;
using IrcDotNet;

namespace BallouBot
{
	public static class ConnectionEvents
	{
		public static void OnRegistered(object sender, EventArgs e)
		{
			var client = (IrcClient)sender;

			client.LocalUser.NoticeReceived += UserEvents.IrcClient_LocalUser_NoticeReceived;
			client.LocalUser.MessageReceived += UserEvents.IrcClient_LocalUser_MessageReceived;
			client.LocalUser.JoinedChannel += UserEvents.IrcClient_LocalUser_JoinedChannel;
			client.LocalUser.LeftChannel += UserEvents.IrcClient_LocalUser_LeftChannel;
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