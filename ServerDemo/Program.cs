using Core.Log;
using Microsoft.Extensions.DependencyInjection;
using Server;
using Server.Database;
using ServerDemo;
using ServerDemo.Repositories;
using Microsoft.Extensions.Configuration;

try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();

    var connectionString  = configuration.GetConnectionString("DefaultConnection");
    
    var serviceCollection = new ServiceCollection();

    serviceCollection.AddSingleton(new ServerSettings()
    {
        ServerId        = 1,
        ServerName      = "DemoServer",
        Port            = 10001,
        MaxSessionCount = 2000,
        HeartBeat       = 30,
    });
    serviceCollection.AddSingleton<DemoServer>();

    serviceCollection.AddSingleton<IDbContext>(new MySqlDbContext(connectionString));
    serviceCollection.AddSingleton<UserRepository>();

    var serviceProvider = serviceCollection.BuildServiceProvider();

    var server = serviceProvider.GetRequiredService<DemoServer>();
    
    server.Start();
}
catch (Exception ex)
{
    Log.Error(ex.ToString());
}