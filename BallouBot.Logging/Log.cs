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

		public void Error(Exception e, string template = "", params object[] props)
		{
			_logger.Error(e, template, props);
		}

		public void Info(string message, params object[] props)
		{
			_logger.Information(message, props);
		}
	}
}