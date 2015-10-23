﻿using BallouBot.Config;
using BallouBot.Interfaces;
using IrcDotNet;

namespace BallouBot.Core
{
	public class Bot
	{
		private const string IrcTwitchTv = "irc.twitch.tv";
		public static bool IsRunning = false;
		private Connection _connection;
		private EventLoop _loop;
		private object _lock = new object();

		public void Start()
		{
			lock (_lock)
			{
				if (!IsRunning)
				{
					var commandQueue = PluginStore.Container.GetExport<ICommandQueue>();
					var config = PluginStore.Container.GetExport<IConfig>().Value;

					var registrationInfo = new IrcUserRegistrationInfo()
					{
						NickName = config.Nickname,
						Password = config.Password,
						UserName = config.Nickname
					};

					_connection = new Connection();
					_connection.Connect(IrcTwitchTv, registrationInfo);

					_loop = new EventLoop();
					_loop.Start(_connection.Client, commandQueue.Value);

					IsRunning = true;
				}
			}
			
		}

		public void Stop()
		{
			lock (_lock)
			{
				if (IsRunning)
				{
					_connection.Dispose();
					_loop.Stop();
				}
			}
		}

	}
}