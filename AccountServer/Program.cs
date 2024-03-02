using AccountServer.Repositories;
using AccountServer.Repositories.Data;
using Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Server;
using Server.Database;

try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();

    var connectionString = configuration.GetConnectionString("DefaultConnection");

    var serviceCollection = new ServiceCollection();

    serviceCollection.AddSingleton(new ServerSettings()
    {
        ServerId           = 1,
        ServerName         = "DemoServer",
        Port               = 10001,
        MaxConnectionCount = 2000,
        HeartBeatInterval  = 150_000,
    });
    serviceCollection.AddSingleton<AccountServer.AccountServer>();

    serviceCollection.AddSingleton<IDbContext>(new MySqlDbContext(connectionString));
    serviceCollection.AddSingleton<MigrateTool>();
    serviceCollection.AddSingleton<AccountRepository>();

    var serviceProvider = serviceCollection.BuildServiceProvider();

    var migrateTool = serviceProvider.GetRequiredService<MigrateTool>();
    migrateTool.Migrate(typeof(Account));

    var server = serviceProvider.GetRequiredService<AccountServer.AccountServer>();
    server.Start();
}
catch (Exception ex)
{
    Log.Error(ex.ToString());
}