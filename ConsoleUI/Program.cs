using BallouBot;
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
			PluginStore.InitializePluginStore();
			var commandQueue = PluginStore.Container.GetExport<ICommandQueue>();
			var config = PluginStore.Container.GetExport<IConfig>().Value;
			
			var registrationInfo = new IrcUserRegistrationInfo()
			{
				NickName = config.Nickname,
				Password = config.Password,
				UserName = config.Nickname
			};

			var connection = new Connection();
			connection.Connect(IrcTwitchTv, registrationInfo);

			var loop = new EventLoop();
			loop.Start(connection.Client, commandQueue.Value);
		}
	}
}
