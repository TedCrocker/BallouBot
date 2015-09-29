using System.Linq;
using System.Text.RegularExpressions;

namespace BallouBot.Core
{
	public static class MessageParser
	{
		private static Regex _regex;

		private static Regex Regex
		{
			get
			{
				if (_regex == null)
				{
					_regex = new Regex(@"(?::(?<Prefix>[^ ]+) +)?(?<Command>[^ :]+)(?<middle>(?: +[^ :]+))*(?<coda> +:(?<trailing>.*)?)?");
				}

				return _regex;
			}
		}

		public static Message ParseIrcMessage(string rawMessage)
		{
			var match = Regex.Match(rawMessage);
			var message = new Message
			{
				Command = match.Groups["Command"].Value,
				Parameters = match.Groups["middle"].Captures.
					OfType<Capture>().
					Select(c => c.Value.Trim()),
				Prefix = match.Groups["Prefix"].Value,
				RawMessage = rawMessage,
				Suffix = match.Groups["trailing"].Value
			};

			return message;
		}
	}
}