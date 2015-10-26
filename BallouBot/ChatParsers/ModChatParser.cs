using System.Threading.Tasks;
using BallouBot.Data;
using BallouBot.Interfaces;

namespace BallouBot.ChatParsers
{
	public abstract class ModChatParser
	{
		protected readonly ICommandQueue _commandQueue;
		protected IDataSource _dataSource;

		protected ModChatParser(ICommandQueue commandQueue, IDataSource dataSource)
		{
			_dataSource = dataSource;
			_commandQueue = commandQueue;
		}

		protected async Task<bool> IsUserMod(string userId, string channel)
		{
			var isMod = false;
			var user = await _dataSource.Repository<User>().Get(userId);

			if (user?.Channels != null && user.Channels.ContainsKey(channel) && user.Channels[channel].IsModerator)
			{
				isMod = true;
			}
			return isMod;
		}
	}
}