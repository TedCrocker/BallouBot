using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
			Assert.Equal(message.Command, 1);
		}

	}
}
