using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BallouBot.Config;
using BallouBot.Core;
using BallouBot.Interfaces;
using Swordfish.NET.Collections;

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
		private readonly ConcurrentObservableCollection<string> _itemsSource;

		public MainWindow()
		{
			InitializeComponent();

			_itemsSource = new ConcurrentObservableCollection<string>();
			var observer = new ConsoleWindowLogObserver(_itemsSource);
			Logging.Log.Observer = observer;

			BindingOperations.EnableCollectionSynchronization(_itemsSource, ConsoleWindowLogObserver.Lock);
			lstConsoleWindow.ItemsSource = _itemsSource;
			_itemsSource.CollectionChanged += (sender, args) =>
			{
				try
				{
					var lastItem = lstConsoleWindow.Items[lstConsoleWindow.Items.Count - 1];
					lstConsoleWindow.ScrollIntoView(lastItem);
				}
				catch (Exception e)
				{
				}
			};

			_bot = new Bot();
		}

		private void btnStartStop_Click(object sender, RoutedEventArgs e)
		{
			btnStartStop.IsEnabled = false;
			if (!_botStarted)
			{
				_itemsSource.Clear();
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
