using LittleOnlineGameServer;
using LittleOnlineGameServer.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Server;
using Server.Database;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();


var serviceCollection = new ServiceCollection();

serviceCollection.AddSingleton(new ServerSettings()
{
    ServerId           = 10001,
    ServerName         = "Little Online Game Server",
    Port               = 11001,
    MaxConnectionCount = 2000,
    HeartBeatInterval  = 150_000,
    PrometheusPort     = 20001,
});
serviceCollection.AddSingleton<LOGServer>();


#region - DB -

var connectionString = configuration.GetConnectionString("DefaultConnection");
serviceCollection.AddSingleton<IDbContext>(new MySqlDbContext(connectionString));
serviceCollection.AddSingleton<AccountRepository>();

#endregion

var serviceProvider = serviceCollection.BuildServiceProvider();

var server = serviceProvider.GetRequiredService<LOGServer>();
server.Start();