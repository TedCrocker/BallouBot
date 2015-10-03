using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BallouBot.Data;

namespace BallouBotTests.Mocks
{
	public class MockRepository<T> : IRepository<T> where T : class, new()
	{
		public IDictionary<string, T> ObjectCache;
		public MockRepository()
		{
			ObjectCache = new ConcurrentDictionary<string, T>();
		} 

		public async Task<IEnumerable<T>> FindAll()
		{
			return ObjectCache.Values;
		}

		public async Task Create(T instance)
		{
			ObjectCache.Add(instance.GetHashCode().ToString(), instance);
		}

		public async Task Update(object id, T instance)
		{
			
		}

		public async Task<T> Get(string id)
		{
			if (ObjectCache.ContainsKey(id))
			{
				return ObjectCache[id];
			}
			return null;
		}
	}
}