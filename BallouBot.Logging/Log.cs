using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Threading;


namespace BallouBot.Logging
{
	public class Log : ILog
	{
		private readonly ILogger _logger;
		public static IObserver<LogEvent> Observer;

		public Log()
		{
			var logConfig = new LoggerConfiguration()
				.WriteTo.ColoredConsole()
				.WriteTo.RollingFile("Logs\\balloubot");

			if (Observer != null)
			{
				logConfig.WriteTo.Observers(observable => observable.Subscribe(Observer));
			}

			_logger = logConfig.CreateLogger();
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