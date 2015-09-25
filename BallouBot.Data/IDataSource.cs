using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace BallouBot.Data
{
	public interface IDataSource
	{
		IRepository<T> Repository<T>() where T : class, new();
	}
}