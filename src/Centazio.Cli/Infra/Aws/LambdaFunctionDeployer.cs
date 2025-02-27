namespace Centazio.Cli.Infra.Aws;

public interface ILambdaFunctionDeployer {

  Task<object> Deploy(FunctionProjectMeta project, string function);

}

public class LambdaFunctionDeployer : ILambdaFunctionDeployer {

  public Task<object> Deploy(FunctionProjectMeta project, string function) => throw new NotImplementedException();

}