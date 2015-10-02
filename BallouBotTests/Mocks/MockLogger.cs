using System;
using System.Linq;
using System.Text;
using BallouBot.Logging;

namespace BallouBotTests.Mocks
{
	public class MockLogger : ILog
	{
		public void Error(Exception e, string template = "", params object[] props)
		{
			
		}

		public void Info(string message, params object[] props)
		{
			
		}
	}
}
