using System;
using System.Threading.Tasks;
using BallouBot.ChatParsers;
using BallouBot.Core;
using BallouBot.Data;
using BallouBot.Interfaces;

namespace BallouBot.PollPlugin
{
	public class PollHandler : ModChatParser, IChatParser
	{
		private static DateTime PreviousPoll = DateTime.MinValue;
		public PollHandler(ICommandQueue commandQueue, IDataSource dataSource) : base(commandQueue, dataSource)
		{
		}
		
		// poll syntax: !poll "Question"; "Option 1"; "Option 2"
		public async Task ReceiveMessage(Message message)
		{
			if (message.Command == Constants.PrivateMessageCommand && message.Suffix.StartsWith("!poll"))
			{
				if ((DateTime.Now - PreviousPoll).TotalMinutes > 5)
				{
					PreviousPoll = DateTime.Now;
					var isUserMod = await IsUserMod(message.User, message.Channel);
					if (isUserMod)
					{
						var pollPostModel = PollHelpers.MapStringToPostModel(message.Suffix);

						if (pollPostModel.Item2.Count > 1)
						{
							var poll = PluginStore.Container.GetExport<IPoll>().Value;
							var url = await poll.Create(pollPostModel.Item1, pollPostModel.Item2);
							_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, $"{pollPostModel.Item1} :: {url}"));
						}
						else
						{
							_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, "You must have at least two options!"));
						}
					}
				}
				else
				{
					var minutesLeft = 5 - (DateTime.Now - PreviousPoll).TotalMinutes;
					_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, $"You must wait {minutesLeft} minutes before you can create another poll."));
				}
				
			}
		}
	}
}