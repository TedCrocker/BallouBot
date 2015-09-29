using System;
using System.Threading;
using BallouBot.Logging;
using IrcDotNet;

namespace BallouBot.Core
{
	public class Connection : IDisposable
	{
		public TwitchIrcClient Client;
		private ILog _logger;
		public bool IsConnected { get; set; }

		public Connection()
		{
			Client = new TwitchIrcClient();
			Client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
			Client.Connected += ConnectionEvents.OnConnected;
			Client.Disconnected += ConnectionEvents.OnDisconnected;
			Client.Registered += ConnectionEvents.OnRegistered;
			Client.ConnectFailed += ConnectionEvents.OnConnectFailed;
			Client.RawMessageReceived += ConnectionEvents.RawMessageReceived;
			_logger = PluginStore.Container.GetExport<ILog>().Value;
		}

		public void Connect(string serverString, IrcRegistrationInfo registrationInfo)
		{

			_logger.Info("[BALLOUBOT] Attempting to connect");
			using (var registeredEvent = new ManualResetEventSlim(false))
			{
				using (var connectedEvent = new ManualResetEventSlim(false))
				{
					Client.Connected += (sender, args) => connectedEvent.Set();
					Client.Registered += (sender, args) => registeredEvent.Set();
					
					Client.Connect(serverString, false, registrationInfo);
					if (!connectedEvent.Wait(1000))
					{
						Client.Dispose();
						throw new Exception("[BALLOUBOT] Could not connect.");
					}
				}
				
				if (!registeredEvent.Wait(10000))
				{
					Client.Dispose();
					throw new Exception("[BALLOUBOT] Could not register.");
				}
			}
			IsConnected = true;

			_logger.Info("[BALLOUBOT] Connected and Registered.");
		}

		public void Dispose()
		{
			Client?.Dispose();
		}
	}
}
