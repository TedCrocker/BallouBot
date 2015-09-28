using System.Collections.Generic;
using System.Threading.Tasks;

namespace BallouBot.Data
{
	public interface IRepository<T>
	{
		Task<IEnumerable<T>> FindAll();
		Task Create(T instance);
		Task Update(object id, T instance);
		T Get(string id);
	}
}