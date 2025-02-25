using AustinS.TailwindCssTool.Binary;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var app = ConsoleApp
    .Create()
    .ConfigureLogging(logging => logging.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss.fff "))
    .ConfigureServices(
        services =>
        {
            services.AddHttpClient(
                BinaryManager.HttpClientName,
                httpClient =>
                {
                    // User agent is required to make a call to the GitHub API.
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(typeof(Program).Assembly.GetName().Name);
                });

            services.AddSingleton<BinaryManager>().AddSingleton<BinaryProcessFactory>();
        });

await app.RunAsync(args);
