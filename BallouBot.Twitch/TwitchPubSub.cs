using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using BallouBot.Config;
using BallouBot.Twitch.Models;

namespace BallouBot.Twitch
{
	public static class TwitchPubSub
	{
		private const int FOLLOWER_UPDATE_INTERVAL_SECONDS = 60;
		private static Timer FollowerCheckTimer;

		private static IDictionary<string, IDictionary<Guid, Action<IList<Follow>>>> _followSubscriptions;
		private static IDictionary<string, IDictionary<Guid, Action<IList<Follow>>>> FollowSubscriptions
		{
			get
			{
				if (_followSubscriptions == null)
				{
					_followSubscriptions = new ConcurrentDictionary<string, IDictionary<Guid, Action<IList<Follow>>>>();
				}
				return _followSubscriptions;
			}
		}

		public static Guid SubscribeToFollows(string channel, Action<IList<Follow>> actionToCallOnFollowUpdate)
		{
			if (!FollowSubscriptions.ContainsKey(channel))
			{
				FollowSubscriptions.Add(channel, new ConcurrentDictionary<Guid, Action<IList<Follow>>>());
			}

			var guid = Guid.NewGuid();
			FollowSubscriptions[channel].Add(guid, actionToCallOnFollowUpdate);
			return guid;
		}

		public static void UnsubscribeFromFollows(string channel, Guid guid)
		{
			if (FollowSubscriptions.ContainsKey(channel) && FollowSubscriptions[channel].ContainsKey(guid))
			{
				FollowSubscriptions[channel].Remove(guid);
			}
		}

		public static void StartFollowerTimer(IConfig config)
		{
			if (FollowerCheckTimer == null)
			{
				FollowerCheckTimer = new Timer(OnFollowerUpdate, config, 0, 1000*FOLLOWER_UPDATE_INTERVAL_SECONDS);
			}
			else
			{
				FollowerCheckTimer.Change(0, 1000 * FOLLOWER_UPDATE_INTERVAL_SECONDS);
			}
		}

		public static void StopFollowerTimer()
		{
			if (FollowerCheckTimer != null)
			{
				FollowerCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}
		}

		private async static void OnFollowerUpdate(object state)
		{
			var config = state as IConfig;
			foreach (var keyVal in FollowSubscriptions)
			{
				var client = new TwitchRestClient(config);
				var follows = await client.GetFollowers(keyVal.Key);
				foreach (var actions in FollowSubscriptions[keyVal.Key])
				{
					if (follows != null) actions.Value(follows);
				}
			}
		}
	}
}