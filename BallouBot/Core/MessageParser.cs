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

		private static Message HandleOldStyleMessage(string rawMessage)
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

		private static Message HandleV3Message(string rawMessage)
		{
			var firstSpace = rawMessage.IndexOf(' ');
			var message = HandleOldStyleMessage(rawMessage.Substring(firstSpace));

			var tags = rawMessage.Substring(1, firstSpace).Split(';');
			foreach (var tag in tags)
			{
				var tagItems = tag.Split('=');
				if (tagItems.Length == 2)
				{
					message.Tags.Add(tagItems[0], tagItems[1]);
				}
			}

			return message;
		}

		public static Message ParseIrcMessage(string rawMessage)
		{
			if (rawMessage.StartsWith("@"))
			{
				return HandleV3Message(rawMessage);
			}

			return HandleOldStyleMessage(rawMessage);
		}
	}
}