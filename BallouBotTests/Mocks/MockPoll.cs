using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BallouBot.PollPlugin;

namespace BallouBotTests.Mocks
{
	public class MockPoll : IPoll
	{
		public MockPoll() { }

		public async Task<string> Create(string title, IList<string> options)
		{
			return "Correct url!";
		}

		public Task<PollResult> Fetch(string id)
		{
			throw new NotImplementedException();
		}
	}
}