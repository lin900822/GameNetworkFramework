using Log;
using Microsoft.Extensions.DependencyInjection;
using Server;
using Server.Database;
using ServerDemo;
using ServerDemo.Repositories;

try
{
    var connectString = @"server=localhost;port=3306;database=gameserver;SslMode=None;uid=root;pwd=root;Allow User Variables=true";

    var serviceCollection = new ServiceCollection();

    serviceCollection.AddSingleton(new ServerSettings()
    {
        ServerId        = 1,
        ServerName      = "DemoServer",
        Port            = 10001,
        MaxSessionCount = 100,
        HeartBeat       = 300000,
    });
    serviceCollection.AddSingleton<DemoServer>();

    serviceCollection.AddSingleton<IDbContext>(new MySqlDbContext(connectString));
    serviceCollection.AddSingleton<UserRepository>();

    var serviceProvider = serviceCollection.BuildServiceProvider();

    var server = serviceProvider.GetRequiredService<DemoServer>();
    
    server.Start();
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}