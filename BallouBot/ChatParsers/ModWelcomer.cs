using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BallouBot.Data;
using BallouBot.Interfaces;
using BallouBot.Logging;

namespace BallouBot.ChatParsers
{
	public class ModWelcomer : IChatParser
	{
		private readonly ICommandQueue _commandQueue;
		private readonly IDataSource _dataSource;
		private ILog _logger;
		private readonly IList<string> _honorifics = new List<string>()
		{
			"san",
			"sama",
			"kun",
			"chan",
			"sensei",
			"senpai",
		}; 

		public ModWelcomer(ICommandQueue commandQueue, IDataSource dataSource, ILog logger)
		{
			_logger = logger;
			_dataSource = dataSource;
			_commandQueue = commandQueue;
		}

		public async Task ReceiveMessage(Message message)
		{
			if (!message.User.Contains("twitch") && !message.User.Contains("balloubot"))
			{
				var user = await _dataSource.Repository<User>().Get(message.User);
				if (user != null)
				{
					if (ShouldSendFirstTimeWelcomeMessage(message, user))
					{
						var honorific = _honorifics.OrderBy(n => Guid.NewGuid()).First();
						_commandQueue.EnqueueCommand("PRIVMSG " + message.Channel + " :Welcome to the channel " + user.Name + honorific + ".");
					}
					else if (ShouldSendWelcomeBackMessage(message, user))
					{
						var honorific = _honorifics.OrderBy(n => Guid.NewGuid()).First();
						_commandQueue.EnqueueCommand("PRIVMSG " + message.Channel + " :Welcome back " + user.Name + honorific + ".");
					}

					user.Data[message.Channel + "-lastMessage"] = DateTime.UtcNow;
					await _dataSource.Repository<User>().Update(message.User, user);
				}
			}
		}

		private static bool ShouldSendFirstTimeWelcomeMessage(Message message, User user)
		{
			return !user.Data.ContainsKey(message.Channel + "-lastMessage");
		}

		private static bool ShouldSendWelcomeBackMessage(Message message, User user)
		{
			var shouldSendMessage = false;
			var time = (DateTime) user.Data[message.Channel + "-lastMessage"];
			if ((DateTime.UtcNow - time).TotalHours > 8)
			{
				shouldSendMessage = true;
			}
			
			return shouldSendMessage;
		}
	}
}