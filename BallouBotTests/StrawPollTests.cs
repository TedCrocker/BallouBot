using System.Collections.Generic;
using BallouBot.Poll;
using Xunit;

namespace BallouBotTests
{
	public class StrawPollTests
	{
		[Fact]
		public async void CanCreatePoll()
		{
			var poll = new StrawPoll();
			var id = await poll.Create("Higgildy", new List<string> {"piggildy", "poo"});

			Assert.NotSame("", id);
		}

		[Fact]
		public async void CanFetchPoll()
		{
			var poll = new StrawPoll();
			var id = await poll.Create("Higgildy", new List<string> { "piggildy", "poo" });

			Assert.NotSame("", id);

			var pollModel = await poll.Fetch(id);
			Assert.NotNull(pollModel);
		}
	}
}