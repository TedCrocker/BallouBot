using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BallouBot.Data
{
	public class MongoRepository<T> : IRepository<T> where T : class, new()
	{
		private IMongoDatabase _database;
		private IDictionary<string, T> _objectCache;
		private bool _cacheHasBeenBuilt = false;
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
			_objectCache = new ConcurrentDictionary<string, T>();
			BuildCache();
			_cacheHasBeenBuilt = true;
		}

		public async Task<IEnumerable<T>> FindAll()
		{
			return _objectCache.Values;
		}

		public T Get(string id)
		{
			if (_objectCache.ContainsKey(id))
			{
				return _objectCache[id];
			}

			return null;
		}

		public async Task Create(T entity)
		{
			await Collection.InsertOneAsync(entity);
			var id = entity.ToBsonDocument()["_id"].AsString;
			_objectCache.Add(id, entity);
		}

		public async Task Update(object id, T instance)
		{
			var idQuery = string.Format("{{_id:\"{0}\"}}", id.ToString());
			await Collection.FindOneAndReplaceAsync<T>(idQuery, instance);
		}

		private async void BuildCache()
		{
			var all = await Collection.Find(new BsonDocument()).ToListAsync();
			foreach (var obj in all)
			{
				var id = obj.ToBsonDocument()["_id"].AsString;
				_objectCache.Add(id, obj);
			}
		}

	}
}