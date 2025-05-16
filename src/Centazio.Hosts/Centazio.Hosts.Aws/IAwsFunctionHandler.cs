using Amazon.Lambda.Core;

namespace Centazio.Hosts.Aws;

public interface IAwsFunctionHandler {
  Task<string> Handle(ILambdaContext context);
}