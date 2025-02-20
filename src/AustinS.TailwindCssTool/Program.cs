using AustinS.TailwindCssTool.Binary;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BinaryManager = AustinS.TailwindCssTool.Binary.BinaryManager;

var app = ConsoleApp
    .Create()
    .ConfigureLogging(logging => logging.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss.fff "))
    .ConfigureServices(services => { services.AddSingleton<BinaryManager>().AddSingleton<BinaryProcessFactory>(); });

await app.RunAsync(args);
