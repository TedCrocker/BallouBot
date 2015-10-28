using System.Collections.Generic;
using System.Threading.Tasks;

namespace BallouBot.PollPlugin
{
	public interface IPoll
	{
		Task<string> Create(string title, IList<string> options);
		Task<PollResult> Fetch(string id);
	}
}