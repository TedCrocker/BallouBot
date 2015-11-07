using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BallouBot.Config;
using BallouBot.Twitch.Models;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Deserializers;

namespace BallouBot.Twitch
{
	public class TwitchRestClient
	{
		private readonly RestClient _restClient;
		private IConfig _config;
		private const string _TWITCH_API_URL = "https://api.twitch.tv/kraken";

		public TwitchRestClient(IConfig config)
		{
			_config = config;
			_restClient = new RestClient(_TWITCH_API_URL);
			_restClient.AddHandler("application/json", new JsonDeserializer());
			_restClient.AddDefaultHeader("Accept", "application/vnd.twitchtv.v3+json");
		}

		public async Task<IList<Follow>> GetFollowers(string channel)
		{
			var request = GetRequest("/channels/{channel}/follows", Method.GET);
			request.AddUrlSegment("channel", channel);

			var response = await _restClient.ExecuteTaskAsync<FollowResponse>(request);
			return response.Data.follows;
		}

		private RestRequest GetRequest(string url, Method method)
		{
			RestRequest request = new RestRequest(url, method);
			request.AddHeader("Client-ID", _config.TwitchClientID);
			request.AddHeader("Authorization", $"OAuth {_config.TwitchClientOauth}");

			return request;
		}
	}
}