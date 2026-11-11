using Centazio.Hosts.Aws;
using {{it.FunctionNamespace}};
using {{it.FunctionNamespace}}.Aws;

var host = new AwsHost({{it.Environments}}, typeof({{it.ClassName}}));
await host.Init(new {{it.ClassName}}Handler(host));
