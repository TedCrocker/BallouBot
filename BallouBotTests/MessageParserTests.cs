using System.Runtime.Serialization;
using BallouBot;
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
		public void CanParsePrivateMessages()
		{
			var privMessage = ":ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :books";
			var message = MessageParser.ParseIrcMessage(privMessage);

			Assert.NotNull(message);
			Assert.Equal(message.User, "ballouthebear");
			Assert.Equal(message.Channel, "#ballouthebear");
		}
	}
}
