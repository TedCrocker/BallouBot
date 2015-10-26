using System.Threading.Tasks;
using BallouBot.Data;
using BallouBot.Interfaces;

namespace BallouBot.ChatParsers
{
	public class PollHandler : ModChatParser, IChatParser
	{
		public PollHandler(ICommandQueue commandQueue, IDataSource dataSource) : base(commandQueue, dataSource)
		{
		}

		public async Task ReceiveMessage(Message message)
		{
			if (message.Command == Constants.PrivateMessageCommand && message.Suffix.StartsWith("!poll"))
			{
				var isUserMod = await IsUserMod(message.User, message.Channel);
                if (isUserMod)
				{
					
				}
			}
		}
	}
}