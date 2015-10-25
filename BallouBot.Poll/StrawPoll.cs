using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BallouBot.Poll
{
	public class StrawPoll : IPoll
	{
		private const string StrawPollUrl = "https://strawpoll.me/api/v2/polls/";
		public async Task<string> Create(string title, IList<string> options)
		{
			var model = new StrawPollPostModel()
			{
				options = options,
				title = title
			};

			var httpClient = new HttpClient();
			var stringPost = JsonConvert.SerializeObject(model);
			var httpContent = new StringContent(stringPost, Encoding.UTF8, "application/json");

			var response = await httpClient.PostAsync(StrawPollUrl, httpContent);
			var responseContent = response.Content.ReadAsStringAsync();
			var responseModel = JsonConvert.DeserializeObject<StrawPollPostResultModel>(responseContent.Result);

			return responseModel.id;
		}

		public async Task<PollResult> Fetch(string id)
		{
			var httpClient = new HttpClient();
			var url = StrawPollUrl + id;
			var response = await httpClient.GetAsync(url);
			var responseContent = response.Content.ReadAsStringAsync();
			var responseModel = JsonConvert.DeserializeObject<PollResult>(responseContent.Result);

			return responseModel;
		}
	}
}