using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BallouBot.Interfaces;

namespace BallouBot.ChatParsers
{
	public class ConsoleHandler : IChatParser
	{
		private readonly ICommandQueue _commandQueue;
		private bool _running = true;
		
		public ConsoleHandler(ICommandQueue commandQueue)
		{
			_commandQueue = commandQueue;
			Thread thread = new Thread(ReadConsoleInput);
			thread.IsBackground = true;
			thread.Start();
		}

		public void ReadConsoleInput()
		{
			//while (_running)
			//{
			//	var command = Console.ReadLine();
			//	_commandQueue.EnqueueCommand(command);
			//}
		}

		public async Task ReceiveMessage(Message message)
		{
		}
	}
}