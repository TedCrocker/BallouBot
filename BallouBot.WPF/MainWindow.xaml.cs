using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BallouBot.Config;
using BallouBot.Core;
using BallouBot.Interfaces;

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
			var observer = new ConsoleWindowLogObserver(strings);
			Logging.Log.Observer = observer;

			BindingOperations.EnableCollectionSynchronization(strings, ConsoleWindowLogObserver.Lock);
			lstConsoleWindow.ItemsSource = strings;
			_bot = new Bot();
			PluginStore.InitializePluginStore();
		}

		private void btnStartStop_Click(object sender, RoutedEventArgs e)
		{
			btnStartStop.IsEnabled = false;
			if (!_botStarted)
			{
				_bot.Start();
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
}
