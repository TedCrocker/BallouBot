using BallouBot;
using BallouBot.Core;
using Xunit;

namespace BallouBotTests
{
	public class MessageHelpersTests
	{
		[Fact]
		public void PrivateMessageFormattedCorrectly()
		{
			var message = MessageParser.ParseIrcMessage(":ballouthebear!ballouthebear@ballouthebear.tmi.twitch.tv PRIVMSG #ballouthebear :books");
			var outgoingMessage = "Hello to you!";

			var formattedMessage = MessageHelpers.PrivateMessage(message, outgoingMessage);
			Assert.Equal(formattedMessage, "PRIVMSG #ballouthebear :Hello to you!");
		}
	}
}