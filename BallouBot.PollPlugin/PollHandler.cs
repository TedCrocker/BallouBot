using System.Threading.Tasks;
using BallouBot.ChatParsers;
using BallouBot.Core;
using BallouBot.Data;
using BallouBot.Interfaces;

namespace BallouBot.PollPlugin
{
	public class PollHandler : ModChatParser, IChatParser
	{
		public PollHandler(ICommandQueue commandQueue, IDataSource dataSource) : base(commandQueue, dataSource)
		{
		}

		// poll syntax: !poll "Question"; "Option 1"; "Option 2"
		public async Task ReceiveMessage(Message message)
		{
			if (message.Command == Constants.PrivateMessageCommand && message.Suffix.StartsWith("!poll"))
			{
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
		}
	}
}