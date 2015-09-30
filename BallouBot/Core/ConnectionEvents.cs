using System;
using BallouBot.Interfaces;
using IrcDotNet;

namespace BallouBot.Core
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
			Console.WriteLine("Connected.");
			var commandQueue = PluginStore.Container.GetExport<ICommandQueue>().Value;
			commandQueue.EnqueueCommand("CAP REQ :twitch.tv/tags", QueuePriority.High);
			commandQueue.EnqueueCommand("CAP REQ :twitch.tv/commands", QueuePriority.High);
			commandQueue.EnqueueCommand("CAP REQ :twitch.tv/membership", QueuePriority.High);
		}

		public static void OnConnectFailed(object sender, IrcErrorEventArgs e)
		{
			throw new NotImplementedException();
		}
	}
}