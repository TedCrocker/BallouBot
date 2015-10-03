﻿using BallouBot.Config;

namespace BallouBotTests.Mocks
{
	public class MockConfig : IConfig
	{
		public string Nickname { get; set; }
		public string Password { get; set; }
		public string TwitchClientID { get; set; }
		public string TwitchRedirectUrl { get; set; }
	}
}