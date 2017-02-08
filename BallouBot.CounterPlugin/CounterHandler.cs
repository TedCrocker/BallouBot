using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BallouBot.ChatParsers;
using BallouBot.Data;
using BallouBot.Interfaces;

namespace BallouBot.CounterPlugin
{
	//Commands
	//!counter create ${counterName}
	//!counter reset ${counterName}
	//!counter delete ${counterName}
	//${counterName}
	//$-{counterName}
	public class CounterHandler : ModChatParser, IChatParser
	{
		private const string CREATE_COMMAND = "create";
		private const string RESET_COMMAND = "reset";
		private const string DELETE_COMMAND = "delete";
		private const int SECONDS_BETWEEN_UPDATE = 15;

		public CounterHandler(ICommandQueue commandQueue, IDataSource dataSource) : base(commandQueue, dataSource){}

		public async Task ReceiveMessage(Message message)
		{
			if (message.Command == Constants.PrivateMessageCommand && message.Suffix.StartsWith("!counter"))
			{
				if (await IsUserMod(message.User, message.Channel))
				{
					await HandleModeratorCommand(message);
				}
				else
				{
					DisplayMessage("Only mods can create counters.", message);
				}
			}
			else if (message.Command == Constants.PrivateMessageCommand && message.Suffix.StartsWith("$"))
			{
				await HandleUserCommand(message);
			}
		}

		private async Task HandleUserCommand(Message message)
		{
			var channelData = await _dataSource.Repository<CounterChannelData>().Get(message.Channel);
			var command = message.Suffix.Split(' ').First().Trim();
			var subtractCount = false;
			if (command.Substring(1,1).StartsWith("-"))
			{
				command = command.Remove(1, 1);
				subtractCount = true;
			}

			var counterData = channelData?.Counters.FirstOrDefault(c => c.Name == command);
			if (counterData != null)
			{
				if (subtractCount && await IsUserMod(message.User, message.Channel))
				{
					counterData.Count--;
					await _dataSource.Repository<CounterChannelData>().Update(channelData.Id, channelData);
					DisplayMessage($"Subtracted: {counterData.UpdateMessage.Replace("#", counterData.Count.ToString())}", message);
				}
				else if ((DateTime.Now - counterData.LastUpdate).TotalSeconds > SECONDS_BETWEEN_UPDATE)
				{
					counterData.Count++;
					counterData.LastUpdate = DateTime.Now;
					await _dataSource.Repository<CounterChannelData>().Update(channelData.Id, channelData);
					DisplayMessage($"{counterData.UpdateMessage.Replace("#", counterData.Count.ToString())}", message);
				}
			}
		}

		private async Task HandleModeratorCommand(Message message)
		{
			var strings = message.Suffix.Substring(8).Trim().Split(' ');

			if (strings.Length >= 2)
			{
				var channelData = await _dataSource.Repository<CounterChannelData>().Get(message.Channel);
				if (channelData == null)
				{
					channelData = new CounterChannelData()
					{
						Counters = new List<CounterData>(),
						Id = message.Channel
					};

					await _dataSource.Repository<CounterChannelData>().Create(channelData);
				}

				if (strings[0].ToLower() == CREATE_COMMAND)
				{
					var updateMessage = $"{strings[1].Substring(1)} # times";
					if (strings.Length >= 3)
					{
						updateMessage = string.Join(" ", strings.Skip(2));
					}
					CreateCounter(strings[1], updateMessage, channelData, message);
				}
				else if (strings[0].ToLower() == RESET_COMMAND)
				{
					ResetCounter(strings[1], channelData, message);
				}
				else if (strings[0].ToLower() == DELETE_COMMAND)
				{
					DeleteCounter(strings[1], channelData, message);
				}
				else
				{
					DisplayMessage("", message);
				}
			}
			else
			{
				_commandQueue.EnqueueCommand(MessageHelpers.Whisper(message, "To create a counter: !counter create ${counterName} {optional message where # will be replaced by count}"));
				_commandQueue.EnqueueCommand(MessageHelpers.Whisper(message, "To reset a counter: !counter reset ${counterName}"));
				_commandQueue.EnqueueCommand(MessageHelpers.Whisper(message, "To delete a counter: !counter delete ${counterName}"));
			}
		}

		private void DisplayMessage(string errorMessage, Message message)
		{
			_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, errorMessage));
		}

		private void CreateCounter(string counterName, string updateMessage, CounterChannelData channelData, Message message)
		{
			var counterData = channelData.Counters.FirstOrDefault(c => c.Name == counterName);
			if (counterData == null)
			{
				channelData.Counters.Add(new CounterData()
				{
					Name = counterName,
					UpdateMessage = updateMessage
				});
				_dataSource.Repository<CounterChannelData>().Update(channelData.Id, channelData);
				DisplayMessage($"Counter {counterName} created", message);
			}
			else
			{
				DisplayMessage($"Counter {counterName} already exists.", message);
			}
		}

		private void ResetCounter(string counterName, CounterChannelData channelData, Message message)
		{
			var counterData = channelData.Counters.FirstOrDefault(c => c.Name == counterName);
			if (counterData == null)
			{
				DisplayMessage($"Counter {counterName} does not exist.", message);
			}
			else
			{
				counterData.Count = 0;
				DisplayMessage($"Counter {counterName} reset", message);
				_dataSource.Repository<CounterChannelData>().Update(channelData.Id, channelData);
			}
		}

		private void DeleteCounter(string counterName, CounterChannelData channelData, Message message)
		{
			var counterData = channelData.Counters.FirstOrDefault(c => c.Name == counterName);
			if (counterData == null)
			{
				DisplayMessage($"Counter {counterName} does not exist.", message);
			}
			else
			{
				DisplayMessage("Counter " + counterData.Name + " removed.", message);
				channelData.Counters.Remove(counterData);
				_dataSource.Repository<CounterChannelData>().Update(channelData.Id, channelData);
			}
		}
	}
}