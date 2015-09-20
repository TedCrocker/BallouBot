using System;
using IrcDotNet;

namespace BallouBot
{
	public static class UserEvents
	{
		public static void IrcClient_LocalUser_LeftChannel(object sender, IrcChannelEventArgs e)
		{
			var localUser = (IrcLocalUser)sender;

			e.Channel.UserJoined -= IrcClient_Channel_UserJoined;
			e.Channel.UserLeft -= IrcClient_Channel_UserLeft;
			e.Channel.MessageReceived -= IrcClient_Channel_MessageReceived;
			e.Channel.NoticeReceived -= IrcClient_Channel_NoticeReceived;

			Console.WriteLine("You left the channel {0}.", e.Channel.Name);
		}

		public static void IrcClient_LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
		{
			var localUser = (IrcLocalUser)sender;

			e.Channel.UserJoined += IrcClient_Channel_UserJoined;
			e.Channel.UserLeft += IrcClient_Channel_UserLeft;
			e.Channel.MessageReceived += IrcClient_Channel_MessageReceived;
			e.Channel.NoticeReceived += IrcClient_Channel_NoticeReceived;

			Console.WriteLine("You joined the channel {0}.", e.Channel.Name);
		}

		public static void IrcClient_Channel_NoticeReceived(object sender, IrcMessageEventArgs e)
		{
			var channel = (IrcChannel)sender;

			Console.WriteLine("[{0}] Notice: {1}.", channel.Name, e.Text);
		}

		public static void IrcClient_Channel_MessageReceived(object sender, IrcMessageEventArgs e)
		{
			var channel = (IrcChannel)sender;
			if (e.Source is IrcUser)
			{
				// Read message.
				Console.WriteLine("[{0}]({1}): {2}.", channel.Name, e.Source.Name, e.Text);
			}
			else
			{
				Console.WriteLine("[{0}]({1}) Message: {2}.", channel.Name, e.Source.Name, e.Text);
			}
		}

		public static void IrcClient_Channel_UserLeft(object sender, IrcChannelUserEventArgs e)
		{
			var channel = (IrcChannel)sender;
			Console.WriteLine("[{0}] User {1} left the channel.", channel.Name, e.ChannelUser.User.NickName);
		}

		public static void IrcClient_Channel_UserJoined(object sender, IrcChannelUserEventArgs e)
		{
			var channel = (IrcChannel)sender;
			Console.WriteLine("[{0}] User {1} joined the channel.", channel.Name, e.ChannelUser.User.NickName);
		}

		public static void IrcClient_LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
		{
			var localUser = (IrcLocalUser)sender;

			if (e.Source is IrcUser)
			{
				// Read message.
				Console.WriteLine("({0}): {1}.", e.Source.Name, e.Text);
			}
			else
			{
				Console.WriteLine("({0}) Message: {1}.", e.Source.Name, e.Text);
			}
		}

		public static void IrcClient_LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
		{
			var localUser = (IrcLocalUser)sender;
			Console.WriteLine("Notice: {0}.", e.Text);
		}
	}
}