using System;
using Serilog.Events;

namespace BallouBot.WPF
{
	public class ConsoleWindowLogObserver : IObserver<LogEvent>
	{
		public static object Lock = new object();
		private MTObservableCollection<string> _strings;

		public ConsoleWindowLogObserver(MTObservableCollection<string> strings)
		{
			
			_strings = strings;
		}

		public void OnNext(LogEvent value)
		{
			lock (Lock)
			{
				_strings.Add(value.RenderMessage());
			}
		}

		public void OnError(Exception error)
		{
			lock (Lock)
			{
				_strings.Add(error.Message);
			}
		}

		public void OnCompleted()
		{
			
		}
	}
}