﻿using BallouBot;
using BallouBot.Config;
using BallouBot.Core;
using BallouBot.Data;
using BallouBot.Interfaces;
using IrcDotNet;

namespace ConsoleUI
{
	class Program
	{
		private const string IrcTwitchTv = "irc.twitch.tv";

		static void Main(string[] args)
		{
			var config = new Config();
			var registrationInfo = new IrcUserRegistrationInfo()
			{
				NickName = config.Nickname,
				Password = config.Password,
				UserName = config.Nickname
			};

			PluginStore.InitializePluginStore();

			var connection = new Connection();
			connection.Connect(IrcTwitchTv, registrationInfo);

			var commandQueue = PluginStore.Container.GetExport<ICommandQueue>();
			var loop = new EventLoop();
			loop.Start(connection.Client, commandQueue.Value);
		}
	}
}
