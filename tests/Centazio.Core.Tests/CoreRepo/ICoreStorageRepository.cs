using System.Linq.Expressions;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.CoreRepo;

public interface ICoreStorageRepository: ICoreStorageUpserter {
  Task<E> Get<E>(string id) where E : class, ICoreEntity;
  Task<IEnumerable<E>> Query<E>(Expression<Func<E, bool>> predicate) where E : class, ICoreEntity;
  Task<IEnumerable<E>> Query<E>(string query) where E : class, ICoreEntity;
}