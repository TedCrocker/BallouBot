using BallouBot;
using BallouBot.Config;
using BallouBot.Data;
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

			var setup = new PluginStore();

			var connection = new Connection();
			connection.Connect(IrcTwitchTv, registrationInfo);

			var commandQueue = PluginStore.Container.GetExport<ICommandQueue>();
			var dataStore = PluginStore.Container.GetExport<IDataSource>();
			var loop = new EventLoop();
			loop.Start(connection.Client, commandQueue.Value);
		}
	}
}
