using Core.Logger;
using Microsoft.Extensions.DependencyInjection;
using Server;
using Server.Database;
using ServerDemo;
using ServerDemo.Repositories;
using Microsoft.Extensions.Configuration;
using Server.Repositories;
using ServerDemo.PO;

try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();

    var connectionString = configuration.GetConnectionString("DefaultConnection");

    var serviceCollection = new ServiceCollection();

    serviceCollection.AddSingleton(new ServerSettings()
    {
        ServerId = 1,
        ServerName = "DemoServer",
        Port = 10001,
        MaxSessionCount = 2000,
        HeartBeatInterval = 150_000,
    });
    serviceCollection.AddSingleton<DemoServer>();

    serviceCollection.AddSingleton<IDbContext>(new MySqlDbContext(connectionString));
    serviceCollection.AddSingleton<MigrateTool>();
    serviceCollection.AddSingleton<UserRepository>();

    var serviceProvider = serviceCollection.BuildServiceProvider();

    var migrateTool = serviceProvider.GetRequiredService<MigrateTool>();
    migrateTool.Migrate(typeof(TestPO));
    migrateTool.Migrate(typeof(UserPO));

    var server = serviceProvider.GetRequiredService<DemoServer>();
    server.Start();
}
catch (Exception ex)
{
    Log.Error(ex.ToString());
}