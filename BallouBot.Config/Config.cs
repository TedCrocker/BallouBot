﻿using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace BallouBot.Config
{
	public class Config : IConfig
	{
		public Config()
		{
			var assemblyPath = AppDomain.CurrentDomain.BaseDirectory;

			var path = Path.GetDirectoryName(assemblyPath);
			var filePath = Path.Combine(path, "config.json");
			if (!File.Exists(filePath))
			{
				path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
				filePath = Path.Combine(path, "config.json");
			}
			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException("Could not find the config.json file!");
			}

			var jsonString = File.ReadAllText(filePath);
			JsonConvert.PopulateObject(jsonString, this);
		}

		public string Nickname { get; set; }
		public string Password { get; set; }
		public string TwitchClientID { get; set; }
		public string TwitchClientOauth { get; set; }
		public string TwitchRedirectUrl { get; set; }
		public string[] Channels { get; set; }
	}
}
