using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public abstract class AbstractAwsCentazioCommand<T, S>(string id) : AbstractCentazioCommand<T, S>(id) 
      where T : class, ICommandLimiter<CommandSettings> 
      where S : CommandSettings;