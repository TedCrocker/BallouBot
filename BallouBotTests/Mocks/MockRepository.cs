using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BallouBot.Data;

namespace BallouBotTests.Mocks
{
	public class MockRepository<T> : IRepository<T> where T : class, new()
	{
		public Task<IEnumerable<T>> FindAll()
		{
			throw new NotImplementedException();
		}

		public Task Create(T instance)
		{
			throw new NotImplementedException();
		}

		public Task Update(object id, T instance)
		{
			throw new NotImplementedException();
		}

		public Task<T> Get(string id)
		{
			throw new NotImplementedException();
		}
	}
}