using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace BallouBot.Config
{
	public class Config : IConfig
	{
		public Config()
		{
			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var filePath = Path.Combine(path, "config.json");
			var jsonString = File.ReadAllText(filePath);
			JsonConvert.PopulateObject(jsonString, this);
		}

		public string Nickname { get; set; }
		public string Password { get; set; }
		public string TwitchClientID { get; set; }
		public string TwitchRedirectUrl { get; set; }
		public string[] Channels { get; set; }
	}
}
