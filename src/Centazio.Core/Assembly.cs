global using System.Reflection;
global using Centazio.Core.Misc;
global using Centazio.Core.Runner;
global using Centazio.Core.Ctl.Entities;
global using Serilog;

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Centazio.Core.Tests")]
[assembly: InternalsVisibleTo("Centazio.Cli.Tests")]
[assembly: InternalsVisibleTo("Centazio.Test.Lib")]
[assembly: InternalsVisibleTo("Centazio.E2E.Tests")]