using Microsoft.Extensions.DependencyInjection;
using Server;
using Shared.Logger;
using SnakeBattleServer;

try
{
    var serviceCollection = new ServiceCollection();
    
    serviceCollection.AddSingleton<SnakeBattleApp>();

    serviceCollection.AddSingleton(new ServerSettings()
    {
        ServerId = 11,
        ServerName = "SnakeBattleServer",
        Port = 50011,
        MaxConnectionCount = 2001,
        HeartBeatInterval = 150_000,
        IsNeedCheckOverReceived = false,
        PrometheusPort = 55011,
    });
    serviceCollection.AddSingleton<BattleServer>();

    var serviceProvider = serviceCollection.BuildServiceProvider();
    
    var app = serviceProvider.GetRequiredService<SnakeBattleApp>();
    app.Start();
}
catch (Exception ex)
{
    Log.Error(ex.ToString());
};