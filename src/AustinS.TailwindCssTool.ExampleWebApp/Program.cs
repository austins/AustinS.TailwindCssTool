var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

#if DEBUG
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<AustinS.TailwindCssTool.ExampleWebApp.TailwindWatcher>();
}
#endif

var app = builder.Build();

app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

await app.RunAsync();
