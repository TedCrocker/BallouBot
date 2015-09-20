using System;

namespace BallouBot
{
	public class BasicChatParser : IChatParser
	{
		private ICommandQueue _commandQueue;

		public BasicChatParser(ICommandQueue commandQueue)
		{
			_commandQueue = commandQueue;
		}

		public void ReceiveMessage(string message)
		{
			Console.WriteLine(message);
		}
	}
}