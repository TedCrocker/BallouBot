using System.Windows;
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

		public MainWindow()
		{
			InitializeComponent();
			_bot = new Bot();
			PluginStore.InitializePluginStore();
		}

		private void btnStartStop_Click(object sender, RoutedEventArgs e)
		{
			if (!_botStarted)
			{
				_bot.Start();
				btnStartStop.Content = "Stop";
			}
			else
			{
				_bot.Stop();
				btnStartStop.Content = "Start";
			}
		}
	}
}
