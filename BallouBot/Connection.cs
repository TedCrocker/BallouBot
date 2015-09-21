using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IrcDotNet;

namespace BallouBot
{
	public class Connection : IDisposable
	{
		public TwitchIrcClient Client;
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
		}

		public void Connect(string serverString, IrcRegistrationInfo registrationInfo)
		{
			Console.WriteLine("[BALLOUBOT] Attempting to connect");
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
			
			Console.WriteLine("[BALLOUBOT] Connected and Registered.");
		}

		public void Dispose()
		{
			Client?.Dispose();
		}
	}
}
