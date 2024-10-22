using System.Linq.Expressions;
using Centazio.Core;
using Centazio.Core.CoreRepo;

namespace Centazio.Test.Lib.CoreStorage;

public interface ICoreStorageWithQuery : ICoreStorage {
  Task<List<E>> Query<E>(CoreEntityTypeName coretype, Expression<Func<E, bool>> predicate) where E : class, ICoreEntity;
  Task<List<E>> Query<E>(CoreEntityTypeName coretype, string query) where E : class, ICoreEntity;
}