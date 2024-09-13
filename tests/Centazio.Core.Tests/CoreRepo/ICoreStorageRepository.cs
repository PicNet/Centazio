﻿using System.Linq.Expressions;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Tests.CoreRepo;

public interface ICoreStorageRepository: ICoreStorageUpserter {
  Task<T> Get<T>(string id) where T : class, ICoreEntity;
  Task<IEnumerable<T>> Query<T>(Expression<Func<T, bool>> predicate) where T : class, ICoreEntity;
  Task<IEnumerable<T>> Query<T>(string query) where T : class, ICoreEntity;
}