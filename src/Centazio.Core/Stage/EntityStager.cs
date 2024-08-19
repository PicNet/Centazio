namespace Centazio.Core.Stage;

public class EntityStager(IStagedEntityStore store, IUtcDate now) {
  public async Task Stage(SystemName source, ObjectName obj, string content) => await store.Save(now.Now, source, obj, content);
  public async Task Stage(SystemName source, ObjectName obj, IEnumerable<string> contents) => await store.Save(now.Now, source, obj, contents);

}

