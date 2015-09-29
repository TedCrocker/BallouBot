using System;
using Serilog;

namespace BallouBot.Logging
{
	public class Log : ILog
	{
		private readonly ILogger _logger;

		public Log()
		{
			_logger = new LoggerConfiguration().WriteTo.ColoredConsole().CreateLogger();
		}

		public void Error(Exception e)
		{
			_logger.Error(e, "");
		}

		public void Info(string message)
		{
			_logger.Information(message);
		}
	}
}