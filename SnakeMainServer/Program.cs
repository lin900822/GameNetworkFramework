using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Server;
using Server.Database;
using Shared.Logger;
using SnakeMainServer;
using SnakeMainServer.Repositories;

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
    
    serviceCollection.AddSingleton<SnakeMainApp>();

    serviceCollection.AddSingleton(new ServerSettings()
    {
        ServerId = 1,
        ServerName = "SnakeMainServer",
        Port = 50001,
        MaxConnectionCount = 2001,
        HeartBeatInterval = 150_000,
        IsNeedCheckOverReceived = false,
        PrometheusPort = 55001,
    });
    serviceCollection.AddSingleton<MainServer>();

    serviceCollection.AddSingleton<IDbContext>(new MySqlDbContext(connectionString));
    serviceCollection.AddSingleton<PlayerRepository>();

    var serviceProvider = serviceCollection.BuildServiceProvider();
    
    var app = serviceProvider.GetRequiredService<SnakeMainApp>();
    app.Start();
}
catch (Exception ex)
{
    Log.Error(ex.ToString());
}