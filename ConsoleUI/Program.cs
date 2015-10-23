using BallouBot.Core;

namespace ConsoleUI
{
	class Program
	{
		static void Main(string[] args)
		{
			PluginStore.InitializePluginStore();
			var bot = new Bot();

			bot.Start();
		}
	}
}
