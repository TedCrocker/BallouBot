using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BallouBot.Config;
using BallouBot.Core;
using BallouBot.Interfaces;
using Serilog.Events;

namespace BallouBot.WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private bool _botStarted = false;
		private static Bot _bot = null;
		private Thread _botThread;

		public MainWindow()
		{
			InitializeComponent();

			var strings = new MTObservableCollection<string>();
			

			lstConsoleWindow.ItemsSource = strings;
			Logging.Log.Observer = new ConsoleWindowLogObserver(strings);
			_bot = new Bot();
			PluginStore.InitializePluginStore();
		}

		private void btnStartStop_Click(object sender, RoutedEventArgs e)
		{
			btnStartStop.IsEnabled = false;
			if (!_botStarted)
			{
				_botThread = new Thread(_bot.Start);
				_botThread.IsBackground = true;
				_botThread.Start();
				btnStartStop.Content = "Stop";
				_botStarted = true;
			}
			else
			{
				_bot.Stop();
				btnStartStop.Content = "Start";
				_botStarted = false;
			}
			btnStartStop.IsEnabled = true;
		}
	}

	public class ConsoleWindowLogObserver : IObserver<LogEvent>
	{
		private MTObservableCollection<string> _strings;

		public ConsoleWindowLogObserver(MTObservableCollection<string> strings)
		{
			_strings = strings;
		}

		public void OnNext(LogEvent value)
		{
			_strings.Add(value.RenderMessage());
		}

		public void OnError(Exception error)
		{
			_strings.Add(error.Message);
		}

		public void OnCompleted()
		{
			
		}
	}

	public class MTObservableCollection<T> : ObservableCollection<T>
	{
		public override event NotifyCollectionChangedEventHandler CollectionChanged;
		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			NotifyCollectionChangedEventHandler CollectionChanged = this.CollectionChanged;
			if (CollectionChanged != null)
				foreach (NotifyCollectionChangedEventHandler nh in CollectionChanged.GetInvocationList())
				{
					DispatcherObject dispObj = nh.Target as DispatcherObject;
					if (dispObj != null)
					{
						Dispatcher dispatcher = dispObj.Dispatcher;
						if (dispatcher != null && !dispatcher.CheckAccess())
						{
							dispatcher.BeginInvoke(
								(Action)(() => nh.Invoke(this,
									new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))),
								DispatcherPriority.DataBind);
							continue;
						}
					}
					nh.Invoke(this, e);
				}
		}
	}
}
