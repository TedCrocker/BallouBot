using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace BallouBot.Data
{
	public class DataSource : IDataSource
	{
		private IDictionary<Type, object> _cachedRepositories;
		private MongoClient _client;
		private IMongoDatabase _database;

		public DataSource()
		{
			var connectionString = "mongodb://localhost:27017";
			_client = new MongoClient(connectionString);
			_database = _client.GetDatabase("ballouthebear");
			_cachedRepositories = new Dictionary<Type, object>();
        }

		public IRepository<T> Repository<T>() where T: class, new()
		{
			var type = typeof (T);
			if (!_cachedRepositories.ContainsKey(type))
			{
				_cachedRepositories.Add(type, new MongoRepository<T>(_database));
			}

			return _cachedRepositories[type] as IRepository<T>;
		}
	}
}