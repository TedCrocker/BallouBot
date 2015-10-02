using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BallouBot.Config;
using BallouBot.Core;
using BallouBot.Interfaces;
using BallouBot.Twitch;

namespace BallouBot.ChatParsers
{
	public class UptimeHandler : IChatParser
	{
		private readonly ICommandQueue _commandQueue;
		private IDictionary<string, DateTime> _lastRequests;

		public UptimeHandler(ICommandQueue commandQueue)
		{
			_commandQueue = commandQueue;
			_lastRequests = new ConcurrentDictionary<string, DateTime>();
		}

		public async Task ReceiveMessage(Message message)
		{
			if (message.Command == Constants.PrivateMessageCommand && message.Suffix.ToLower() == "!uptime")
			{
				var sendMessage = true;
				if (_lastRequests.ContainsKey(message.Channel))
				{
					sendMessage = (DateTime.UtcNow - _lastRequests[message.Channel]).TotalMinutes >
					              Constants.TimeBetweenUptimeRequestsInMinutes;
					_lastRequests[message.Channel] = DateTime.UtcNow;
				}
				else
				{
					_lastRequests.Add(message.Channel, DateTime.UtcNow);
				}

				if (sendMessage)
				{
					var twitchApi = PluginStore.Container.GetExport<ITwitchApi>().Value;
					var uptime = await twitchApi.GetUptime(message.Channel.Substring(1));
					if (uptime.HasValue)
					{
						_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, "Uptime: " + uptime.Value.ToString()));
					}
					else
					{
						_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, "The stream currently is not live."));
					}
					
				}
				
			}
			
		}
	}
}