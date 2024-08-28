namespace Centazio.Core.Stage;

// todo: remove
public class EntityStager(IStagedEntityStore store) {
  public async Task Stage(DateTime staged, SystemName source, ObjectName obj, string content) => await store.Save(staged, source, obj, content);
  public async Task Stage(DateTime staged, SystemName source, ObjectName obj, IEnumerable<string> contents) => await store.Save(staged, source, obj, contents);

}

