using System.Runtime.Serialization;
using BallouBot.Core;
using Xunit;

namespace BallouBotTests
{
	public class MessageParserTests
	{
		[Fact]
		public void CanParseMotd()
		{
			var motd = ":tmi.twitch.tv 001 balloubot :Welcome, GLHF!";
			var message = MessageParser.ParseIrcMessage(motd);

			Assert.NotNull(message);
			Assert.Equal(message.Command, "001");
		}

		[Fact]
		public void CanPartJoinResponse()
		{
			var join = ":balloubot!balloubot@balloubot.tmi.twitch.tv JOIN #ballouthebear";
			var message = MessageParser.ParseIrcMessage(join);

			Assert.NotNull(message);
		}

		[Fact]
		public void CutTwitchStuffOffUserName()
		{
			var join = ":balloubot.tmi.twitch.tv!balloubot@balloubot.tmi.twitch.tv JOIN #ballouthebear";
			var message = MessageParser.ParseIrcMessage(join);

			Assert.NotNull(message);
			Assert.Equal(message.User, "balloubot");
		}

		[Fact]
		public void CanParsePrivateMessages()
		{
			var privMessage = ":ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :books";
			var message = MessageParser.ParseIrcMessage(privMessage);

			Assert.NotNull(message);
			Assert.Equal(message.User, "ballouthebear");
			Assert.Equal(message.Channel, "#ballouthebear");
		}

		[Fact]
		public void CanParseUserStateMessage()
		{
			var rawMessage = "@color=;display-name=Balloubot;emote-sets=0;subscriber=0;turbo=0;user-type=mod :tmi.twitch.tv USERSTATE #ballouthebear";
			var message = MessageParser.ParseIrcMessage(rawMessage);

			Assert.NotNull(message);
			Assert.True(message.Tags.ContainsKey("user-type"));
		}
	}
}
