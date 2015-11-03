using System.Dynamic;
using BallouBot.Interfaces;

namespace BallouBot.Core
{
	public class ChatParserContainer
	{
		public bool IsEnabled { get; set; }
		public IChatParser Parser;
		public string Name => Parser.GetType().Name;
	}
}