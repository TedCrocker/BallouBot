using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BallouBot
{
	public static class MessageHelpers
	{
		public static string PrivateMessage(Message incomingMessage, string outgoingMessage)
		{
			var str = $"{Constants.PrivateMessageCommand} {incomingMessage.Channel} :{outgoingMessage}";
			return str;
		}
	}
}
