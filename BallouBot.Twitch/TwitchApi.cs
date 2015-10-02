using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BallouBot.Config;
using TwixelAPI;

namespace BallouBot.Twitch
{
	public class TwitchApi : ITwitchApi
	{
		private readonly Twixel _twixel;

		public TwitchApi(IConfig config)
		{
			_twixel = new Twixel(config.TwitchClientID, config.TwitchRedirectUrl);
		}

		public async Task SetUserInfo(Data.User user)
		{
			var twitchUser = await _twixel.RetrieveUser(user.Id, Twixel.APIVersion.v3);
			user.Name = twitchUser.displayName;
		}

		public async Task<TimeSpan?> GetUptime(string channel)
		{
			TimeSpan? timeSpan = null;
			try
			{
				var stream = await _twixel.RetrieveStream(channel);
				timeSpan = (DateTime.UtcNow - stream.createdAt);
			}
			catch (Exception e)
			{
				
			}
			
			return timeSpan;
		}
	}
}
