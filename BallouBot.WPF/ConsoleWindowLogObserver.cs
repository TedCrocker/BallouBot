using System;
using Serilog.Events;
using Swordfish.NET.Collections;

namespace BallouBot.WPF
{
	public class ConsoleWindowLogObserver : IObserver<LogEvent>
	{
		public static object Lock = new object();
		private ConcurrentObservableCollection<string> _strings;

		public ConsoleWindowLogObserver(ConcurrentObservableCollection<string> strings)
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