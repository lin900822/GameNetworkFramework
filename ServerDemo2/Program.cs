using Microsoft.Extensions.DependencyInjection;
using Server;
using ServerDemo2;
using Shared.Logger;

try
{
    var serviceCollection = new ServiceCollection();

    serviceCollection.AddSingleton(new ServerSettings()
    {
        ServerId = 2,
        ServerName = "DemoServer2",
        Port = 10002,
        MaxConnectionCount = 2000,
        HeartBeatInterval = 300_000,
    });
    serviceCollection.AddSingleton<DemoServer>();

    var serviceProvider = serviceCollection.BuildServiceProvider();

    var server = serviceProvider.GetRequiredService<DemoServer>();

    server.Start();
}
catch (Exception ex)
{
    Log.Error(ex.ToString());
}