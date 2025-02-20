using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Server;
using Server.Database;
using ServerDemo;
using ServerDemo.Repositories;
using Shared.Logger;

try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    
    bool isDbConnected = false;
    while (!isDbConnected)
    {
        using var connection = new MySqlConnection(connectionString);
        try
        {
            connection.Open();
            isDbConnected = true;
        }
        catch (MySqlException ex)
        {
            Log.Warn("Db Connection failed: " + ex.Message);
            Log.Warn("Retrying in 1 seconds...");
            Thread.Sleep(1000);
        }
    }

    var serviceCollection = new ServiceCollection();

    serviceCollection.AddSingleton(new ServerSettings()
    {
        ServerId = 1,
        ServerName = "DemoServer",
        Port = 50001,
        MaxConnectionCount = 2001,
        HeartBeatInterval = 150_000,
        IsNeedCheckOverReceived = false,
        PrometheusPort = 55001,
    });
    serviceCollection.AddSingleton<DemoServer>();

    serviceCollection.AddSingleton<IDbContext>(new MySqlDbContext(connectionString));
    serviceCollection.AddSingleton<MigrateTool>();
    serviceCollection.AddSingleton<UserRepository>();

    var serviceProvider = serviceCollection.BuildServiceProvider();

    // var migrateTool = serviceProvider.GetRequiredService<MigrateTool>();
    // migrateTool.Migrate(typeof(TestPO));
    // migrateTool.Migrate(typeof(UserPO));

    var server = serviceProvider.GetRequiredService<DemoServer>();
    server.Start();
}
catch (Exception ex)
{
    Log.Error(ex.ToString());
}