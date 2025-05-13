namespace Centazio.Core.Secrets;

public interface ISecretsLoader  {
  Task<T> Load<T>(params List<string> environments);
}