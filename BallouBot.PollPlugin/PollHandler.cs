using System;
using System.Threading.Tasks;
using BallouBot.ChatParsers;
using BallouBot.Core;
using BallouBot.Data;
using BallouBot.Interfaces;
using BallouBot.Logging;

namespace BallouBot.PollPlugin
{
	public class PollHandler : ModChatParser, IChatParser
	{
		private static DateTime PreviousPoll = DateTime.MinValue;
		private ILog _logger;

		public PollHandler(ICommandQueue commandQueue, IDataSource dataSource, ILog logger) : base(commandQueue, dataSource)
		{
			_logger = logger;
		}

		// poll syntax: !poll "Question"; "Option 1"; "Option 2"
		public async Task ReceiveMessage(Message message)
		{
			if (message.Command == Constants.PrivateMessageCommand && message.Suffix.StartsWith("!poll"))
			{
				if ((DateTime.Now - PreviousPoll).TotalMinutes > 5)
				{
					var isUserMod = await IsUserMod(message.User, message.Channel);
					if (isUserMod)
					{
						var pollPostModel = PollHelpers.MapStringToPostModel(message.Suffix);

						if (pollPostModel.Item2.Count > 1)
						{
							try
							{
								var poll = PluginStore.Container.GetExport<IPoll>().Value;
								var url = await poll.Create(pollPostModel.Item1, pollPostModel.Item2);
								_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, $"{pollPostModel.Item1} :: {url}"));
								PreviousPoll = DateTime.Now;
							}
							catch (Exception e)
							{
								_logger.Error(e);
                                _commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, "There was an issue creating the poll! So sorry!"));
							}
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