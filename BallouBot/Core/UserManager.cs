using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BallouBot.Data;
using BallouBot.Logging;
using BallouBot.Twitch;

namespace BallouBot.Core
{
	public class UserManager
	{
		private IDataSource _repoStore;
		private ITwitchApi _api;
		private ILog _logger;
		private static IList<string> _useresToIgnore = new List<string>()
		{
			"jtv",
			Constants.TwitchUser
		};

		public UserManager()
		{
			_api = PluginStore.Container.GetExport<ITwitchApi>().Value;
			_repoStore = PluginStore.Container.GetExport<IDataSource>().Value;
			_logger = PluginStore.Container.GetExport<ILog>().Value;
		}

		public async Task UpdateOrCreateUser(Message message)
		{
			var userID = message.User;
			if (message.Command == Constants.ModeCommand)
			{
				userID = message.RawMessage.Trim().Split(' ').Last();
			}
			if (!_useresToIgnore.Contains(userID))
			{
				var user = await GetOrCreateUser(userID);

				if (message.Command == Constants.ModeCommand && message.RawMessage.Contains("+o"))
				{
					await CreateChannelForUser(message, user);
				}
			}
		}

		private async Task<User> GetOrCreateUser(string userID)
		{
			var user = await _repoStore.Repository<User>().Get(userID);
			if (user == null)
			{
				user = await CreateUser(userID);
			}

			return user;
		}

		private async Task<User> CreateUser(string userID)
		{
			User user = new User()
			{
				Id = userID,
				Channels = new ConcurrentDictionary<string, UserChannel>(),
				Data = new ConcurrentDictionary<string, object>()
			};
			
			try
			{
				await _api.SetUserInfo(user);
				await _repoStore.Repository<User>().Create(user);
			}
			catch (Exception e)
			{
				_logger.Error(e);
			}

			return user;
		}

		private async Task CreateChannelForUser(Message parsedMessage, User user)
		{
			if (!user.Channels.ContainsKey(parsedMessage.Channel))
			{
				user.Channels.Add(parsedMessage.Channel, new UserChannel()
				{
					Name = parsedMessage.Channel
				});
				await _repoStore.Repository<User>().Update(user.Id, user);
			}
		}
	}
}