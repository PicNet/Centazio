using System.Linq.Expressions;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.CoreRepo;

public interface ICoreStorageRepository: ICoreStorageUpserter {
  Task<C> Get<C>(string id) where C : class, ICoreEntity;
  Task<IEnumerable<C>> Query<C>(Expression<Func<C, bool>> predicate) where C : class, ICoreEntity;
  Task<IEnumerable<C>> Query<C>(string query) where C : class, ICoreEntity;
}