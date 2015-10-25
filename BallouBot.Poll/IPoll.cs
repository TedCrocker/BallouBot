using System.Collections.Generic;
using System.Threading.Tasks;

namespace BallouBot.Poll
{
	public interface IPoll
	{
		Task<string> Create(string title, IList<string> options);
		Task<PollResult> Fetch(string id);
	}
}