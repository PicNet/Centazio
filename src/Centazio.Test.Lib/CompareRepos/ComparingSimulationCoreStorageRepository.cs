﻿using System.Reflection;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.E2E;

namespace Centazio.Test.Lib.CompareRepos;

public class ComparingSimulationCoreStorageRepository(AbstractCoreStorageRepository repo1, AbstractCoreStorageRepository repo2, Func<ICoreEntity, CoreEntityChecksum> checksum) : AbstractCoreStorageRepository(checksum) {

  public override async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    // var (result1, result2) = (await repo1.Upsert(coretype, entities), await repo2.Upsert(coretype, entities));
    var result1 = await repo1.Upsert(coretype, entities);
    var result2 = await repo2.Upsert(coretype, entities);
    return ValidateAndReturn(result1, result2);
  }

  protected override async Task<List<CoreEntityAndMeta>> GetExistingEntities<E, D>(List<CoreEntityId> coreids) {
    var result1 = await (Task<List<CoreEntityAndMeta>>) repo1.GetType().GetMethod(nameof(GetExistingEntities), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(E), typeof(D)).Invoke(repo1, [coreids])!;
    var result2 = await (Task<List<CoreEntityAndMeta>>) repo2.GetType().GetMethod(nameof(GetExistingEntities), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(E), typeof(D)).Invoke(repo2, [coreids])!;
    return ValidateAndReturn(result1, result2);
  }

  protected override async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite<E, D>(SystemName exclude, DateTime after) {
    var result1 = await (Task<List<CoreEntityAndMeta>>) repo1.GetType().GetMethod(nameof(GetEntitiesToWrite), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(E), typeof(D)).Invoke(repo1, [exclude, after])!;
    var result2 = await (Task<List<CoreEntityAndMeta>>) repo2.GetType().GetMethod(nameof(GetEntitiesToWrite), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(E), typeof(D)).Invoke(repo2, [exclude, after])!;
    return ValidateAndReturn(result1, result2);
  }

  public override async ValueTask DisposeAsync() {
    await repo1.DisposeAsync();
    await repo2.DisposeAsync();
  }

  protected override async Task<E> GetSingle<E, D>(CoreEntityId coreid) {
    var result1 = await (Task<E>) repo1.GetType().GetMethod(nameof(GetSingle), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(E), typeof(D)).Invoke(repo1, [coreid])!;
    var result2 = await (Task<E>) repo2.GetType().GetMethod(nameof(GetSingle), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(E), typeof(D)).Invoke(repo2, [coreid])!;
    return ValidateAndReturn(result1, result2);
  }
  
  private T ValidateAndReturn<T>(T a, T b) {
    Json.ValidateJsonEqual(a, b, repo1.GetType().Name, repo2.GetType().Name);
    return a;
  }
}