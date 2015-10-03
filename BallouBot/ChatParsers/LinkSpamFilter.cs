using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BallouBot.Data;
using BallouBot.Interfaces;

namespace BallouBot.ChatParsers
{
	public class LinkSpamFilter : IChatParser
	{
		private IDataSource _dataSource;
		private ICommandQueue _commandQueue;
		private static Regex _webUrlRegex = new Regex(@"(?i)\b((?:[a-z][\w-]+:(?:/{1,3}|[a-z0-9%])|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s`!()\[\]{};:'"".,<>?«»“”‘’]))");

		public LinkSpamFilter(ICommandQueue commandQueue, IDataSource dataSource)
		{
			_commandQueue = commandQueue;
			_dataSource = dataSource;
		}

		public async Task ReceiveMessage(Message message)
		{
			if (message.Command == Constants.PrivateMessageCommand && _webUrlRegex.IsMatch(message.Suffix))
			{
				var userIsMod = await IsUserMod(message.User, message.Channel);
				if (!userIsMod)
				{
					_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, ".timeout " + message.User + " 1"));
				}
			}
		}

		private async Task<bool> IsUserMod(string userId, string channel)
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