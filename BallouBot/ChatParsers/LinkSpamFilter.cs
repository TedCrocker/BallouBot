using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BallouBot.Data;
using BallouBot.Interfaces;

namespace BallouBot.ChatParsers
{
	public class LinkSpamFilter : IChatParser
	{
		private readonly IDataSource _dataSource;
		private readonly ICommandQueue _commandQueue;
		private static readonly Regex WebUrlRegex = new Regex(@"(?i)\b((?:[a-z][\w-]+:(?:/{1,3}|[a-z0-9%])|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s`!()\[\]{};:'"".,<>?«»“”‘’]))");

		public LinkSpamFilter(ICommandQueue commandQueue, IDataSource dataSource)
		{
			_commandQueue = commandQueue;
			_dataSource = dataSource;
		}

		public async Task ReceiveMessage(Message message)
		{
			if (message.Command == Constants.PrivateMessageCommand)
			{
				if (WebUrlRegex.IsMatch(message.Suffix))
				{
					await HandleMessageWithLink(message);
				}
				if (message.Suffix.Contains("!permit"))
				{
					await HandlePermitUserCommand(message);
				}
			}
		}

		private async Task HandlePermitUserCommand(Message message)
		{
			var userIsMod = await IsUserMod(message.User, message.Channel);
			if (userIsMod)
			{
				var suffixWords = message.Suffix.Split(' ');
				if (suffixWords.Length >= 2)
				{
					var userName = suffixWords[1];
					var user = await _dataSource.Repository<User>().Get(userName);
					if (user != null)
					{
						user.Data[message.Channel + "-hasLinkPermission"] = true;
						await _dataSource.Repository<User>().Update(user.Id, user);
						_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, "User " + userName + " has been permitted to post a link."));
					}
				}
			}
		}

		private async Task HandleMessageWithLink(Message message)
		{
			var userIsModOrPermitted = await IsUserMod(message.User, message.Channel) || await DoesUserHavePermission(message.User, message.Channel);
			if (!userIsModOrPermitted)
			{
				_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, ".timeout " + message.User + " 5"), QueuePriority.High);
				_commandQueue.EnqueueCommand(MessageHelpers.PrivateMessage(message, message.User + " you baka! Don't post links unless permitted!"));
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

		private async Task<bool> DoesUserHavePermission(string userId, string channel)
		{
			var isPermitted = false;
			var user = await _dataSource.Repository<User>().Get(userId);

			if (user.Data.ContainsKey(channel + "-hasLinkPermission"))
			{
				isPermitted = true;
				user.Data.Remove(channel + "-hasLinkPermission");
				await _dataSource.Repository<User>().Get(userId);
			}

			return isPermitted;
		}
	}
}