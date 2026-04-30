using Spectre.Console.Cli;
using VsaTemplate.Scaffold;

var app = new CommandApp<ScaffoldCommand>();
return await app.RunAsync(args);
