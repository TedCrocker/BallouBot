using System.Threading.Tasks;

namespace BallouBot
{
	public interface IChatParser
	{
		Task ReceiveMessage(Message message);
	}
}