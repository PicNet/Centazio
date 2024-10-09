using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.CoreStorage;

public interface ICoreStorageRepository: ICoreStorageUpserter {
  Task<E> Get<E>(CoreEntityType obj, ValidString id) where E : class, ICoreEntity;
  Task<List<E>> Query<E>(CoreEntityType obj, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity;
  Task<List<E>> Query<E>(CoreEntityType obj, string query) where E : class, ICoreEntity;
}
