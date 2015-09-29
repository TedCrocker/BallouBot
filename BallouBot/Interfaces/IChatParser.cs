using System.Threading.Tasks;

namespace BallouBot.Interfaces
{
	public interface IChatParser
	{
		Task ReceiveMessage(Message message);
	}
}