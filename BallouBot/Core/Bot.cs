using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BallouBot.Config;
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

		public Bot()
		{
			InitializePluginStore();
		}

		public void InitializePluginStore()
		{
			PluginStore.InitializePluginStore();
			SetParsers();
		}

		private void SetParsers()
		{
			foreach (var parser in GetAvailableChatParsers())
			{
				RawMessageHandler.Parsers.Add(parser);
			}
		}

		public void SetParser(ICollection<IChatParser> chatParsers)
		{
			var bag = new ConcurrentBag<IChatParser>();
			foreach (var parser in chatParsers)
			{
				bag.Add(parser);
			}

			Interlocked.Exchange(ref RawMessageHandler.Parsers, bag);
		}

		public IList<IChatParser> GetAvailableChatParsers()
		{
			var parsers = PluginStore.Container.GetExports<IChatParser>();
			return parsers.Select(parser => parser.Value).ToList();
		}

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

					foreach (var channel in config.Channels)
					{
						commandQueue.Value.EnqueueCommand("JOIN " + channel);
					}
					
					_loop = new EventLoop();
					var thread = new Thread(() =>
					{
						_loop.Start(_connection.Client, commandQueue.Value);
					});
					thread.SetApartmentState(ApartmentState.STA);
					thread.IsBackground = true;
					thread.Start();

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
					IsRunning = false;
				}
			}
		}

	}
}
