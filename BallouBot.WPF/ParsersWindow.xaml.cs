using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BallouBot.Core;
using BallouBot.Interfaces;

namespace BallouBot.WPF
{
	/// <summary>
	/// Interaction logic for ParsersWindow.xaml
	/// </summary>
	public partial class ParsersWindow : Window
	{
		public ParsersWindow(Bot bot)
		{
			InitializeComponent();

			lstParsers.ItemsSource = bot.GetChatParserContainers();
		}
	}
}
