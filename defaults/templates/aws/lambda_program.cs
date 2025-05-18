using Centazio.Hosts.Aws;
using {{it.FunctionNamespace}};
using {{it.FunctionNamespace}}.Aws;

await AwsHost.Init({{it.Environments}}, new {{it.ClassName}}Handler(), typeof({{it.ClassName}}));
