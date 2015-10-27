using System;
using System.Collections.Generic;
using System.Linq;

namespace BallouBot.Poll
{
	public static class PollHelpers
	{
		public static Tuple<string, IList<string>> MapStringToPostModel(string message)
		{
			string title;
			var pieces = message.Substring(5).Trim().Split(';').ToList();

			title = RemoveStartAndEndQuotationMarks(pieces.First());
			IList<string> options = pieces.Skip(1).Select(RemoveStartAndEndQuotationMarks).ToList();

			return new Tuple<string, IList<string>>(title, options);
		}

		private static string RemoveStartAndEndQuotationMarks(string piece)
		{
			var result = piece;
			if (piece.StartsWith("\""))
			{
				result = result.Substring(1);
			}
			if (piece.EndsWith("\""))
			{
				result = result.Remove(result.Length - 1);
			}

			return result.Trim();
		}
	}
}