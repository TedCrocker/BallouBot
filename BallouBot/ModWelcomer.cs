using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BallouBot.Data;

namespace BallouBot
{
	public class ModWelcomer : IChatParser
	{
		private ICommandQueue _commandQueue;
		private IDataSource _dataSource;

		public ModWelcomer(ICommandQueue commandQueue, IDataSource dataSource)
		{
			_dataSource = dataSource;
			_commandQueue = commandQueue;
		}


		public async Task ReceiveMessage(Message message)
		{
			if (!message.User.Contains("twitch") && !message.User.Contains("balloubot"))
			{
				var userList = await _dataSource.Repository<User>().FindAll();
				User user = null;

				if (!userList.Any(u => u.Name == message.User))
				{
					user = new User
					{
						Data = new Dictionary<string, object>(),
						Name = message.User,
						Id = message.User
					};
					await _dataSource.Repository<User>().Create(user);
				}
				else
				{
					user = userList.First(u => u.Name == message.User);
				}

				var sendMessage = false;
				if (!user.Data.ContainsKey(message.Channel + "-lastMessage"))
				{
					sendMessage = true;
				}
				else
				{
					var time = (DateTime) user.Data[message.Channel + "-lastMessage"];
					if ((DateTime.UtcNow - time).TotalHours > 8)
					{
						sendMessage = true;
					}
				}

				if (sendMessage)
				{
					_commandQueue.EnqueueCommand("PRIVMSG " + message.Channel + " :Welcome back " + user.Name + "san.");
				}

				user.Data[message.Channel + "-lastMessage"] = DateTime.UtcNow;
				await _dataSource.Repository<User>().Update(message.User, user);
			}
		}
	}
}