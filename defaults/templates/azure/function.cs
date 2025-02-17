using Microsoft.Azure.Functions.Worker;
using Centazio.Core.Runner;

namespace [NewAssemblyName];

public class [ClassName]Azure {  
  private static readonly Lazy<Task<IRunnableFunction>> impl;

  static [ClassName]Azure() {    
    impl = new(async () => await new FunctionsInitialiser("[Environment]").Init<[ClassName]>(), LazyThreadSafetyMode.ExecutionAndPublication);
  }

  [Function(nameof([ClassName]))] public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo timer) {    
    await (await impl.Value).RunFunction(); 
  }
}