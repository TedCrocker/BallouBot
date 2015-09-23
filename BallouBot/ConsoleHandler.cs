using System;
using System.Threading;
using BallouBot.Data;

namespace BallouBot
{
	public class ConsoleHandler : IChatParser
	{
		private ICommandQueue _commandQueue;
		private bool _running = true;

		public ConsoleHandler(ICommandQueue commandQueue, IDataSource dataSource)
		{
			_commandQueue = commandQueue;
			Thread thread = new Thread(ReadConsoleInput);
			thread.IsBackground = true;
			thread.Start();
		}

		public void ReadConsoleInput()
		{
			while (_running)
			{
				var command = Console.ReadLine();
				_commandQueue.EnqueueCommand(command);
			}
		}

		public void ReceiveMessage(Message message)
		{
			Console.WriteLine(message.RawMessage);
		}
	}
}