using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BallouBot.Data
{
	public class MongoRepository<T> : IRepository<T> where T : class, new()
	{
		private IMongoDatabase _database;

		private IMongoCollection<T> _collection;
		private IMongoCollection<T> Collection
		{
			get
			{
				if (_collection == null)
				{
					_collection = _database.GetCollection<T>(typeof (T).Name);
				}
				return _collection;
			}
		}

		public MongoRepository(IMongoDatabase database)
		{
			_database = database;
		}

		public async Task<IEnumerable<T>> FindAll()
		{
			return await Collection.Find(new BsonDocument()).ToListAsync();
		}

		public async Task Create(T entity)
		{
			await Collection.InsertOneAsync(entity);
		}

		public async Task Update(object id, T instance)
		{
			var idQuery = string.Format("{{_id:\"{0}\"}}", id.ToString());
			await Collection.FindOneAndReplaceAsync<T>(idQuery, instance);
		}
	}
}