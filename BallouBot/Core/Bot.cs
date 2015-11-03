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
		private readonly object _startLock = new object();
		private readonly object _parserLock = new object();

		public Bot()
		{
			InitializePluginStore();
		}

		public void InitializePluginStore()
		{
			PluginStore.InitializePluginStore();
			UpdateParsers();
		}

		private void UpdateParsers()
		{
			lock (_parserLock)
			{
				foreach (var parser in GetAvailableChatParsers())
				{
					var parserName = parser.GetType().Name;
					if (!RawMessageHandler.Parsers.Select(p => p.ToString()).Contains(parserName))
					{
						RawMessageHandler.Parsers.Add(new ChatParserContainer()
						{
							IsEnabled = true,
							Parser = parser
						});
					}
				}
			}
		}

		public ConcurrentBag<ChatParserContainer> GetChatParserContainers()
		{
			return RawMessageHandler.Parsers;
		}
		
		public IList<IChatParser> GetAvailableChatParsers()
		{
			var parsers = PluginStore.Container.GetExports<IChatParser>();
			return parsers.Select(parser => parser.Value).ToList();
		}

		public void Start()
		{
			lock (_startLock)
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
			lock (_startLock)
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
