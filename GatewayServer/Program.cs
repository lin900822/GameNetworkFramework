using Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Server;

try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();

    var serviceCollection = new ServiceCollection();

    serviceCollection.AddSingleton(new ServerSettings()
    {
        ServerId           = 10001,
        ServerName         = "GatewayServer",
        Port               = 10001,
        MaxConnectionCount = 2000,
        HeartBeatInterval  = 150_000,
        PrometheusPort     = 20001,
    });
    serviceCollection.AddSingleton<GatewayServer.GatewayServer>();

    var serviceProvider = serviceCollection.BuildServiceProvider();

    var server = serviceProvider.GetRequiredService<GatewayServer.GatewayServer>();
    server.Start();
}
catch (Exception ex)
{
    Log.Error(ex.ToString());
}