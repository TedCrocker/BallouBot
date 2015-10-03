using System;
using System.Collections.Generic;
using BallouBot.Data;

namespace BallouBotTests.Mocks
{
	public class MockDataSource : IDataSource
	{
		private readonly IDictionary<Type, object> _cachedRepositories = new Dictionary<Type, object>();
		public IRepository<T> Repository<T>() where T : class, new()
		{
			var type = typeof(T);
			if (!_cachedRepositories.ContainsKey(type))
			{
				_cachedRepositories.Add(type, new MockRepository<T>());
			}

			return _cachedRepositories[type] as IRepository<T>;
		}
	}
}