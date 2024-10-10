using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.CoreStorage;

public interface ICoreStorageRepository: ICoreStorageUpserter {
  Task<E> Get<E>(CoreEntityType coretype, CoreEntityId coreid) where E : class, ICoreEntity;
  Task<List<E>> Query<E>(CoreEntityType coretype, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity;
  Task<List<E>> Query<E>(CoreEntityType coretype, string query) where E : class, ICoreEntity;
}
