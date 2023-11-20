using Log;
using Microsoft.Extensions.DependencyInjection;
using Server;
using Server.Database;
using Server.Repositories;

var connectString = @"server=localhost;port=3306;database=gameserver;SslMode=None;uid=root;pwd=root;Allow User Variables=true";

var serviceCollection = new ServiceCollection();

serviceCollection.AddSingleton<DemoServer>();

serviceCollection.AddSingleton<IDbContext>(new MySqlDbContext(connectString));
serviceCollection.AddSingleton<UserRepository>();

var serviceProvider = serviceCollection.BuildServiceProvider();

var server = serviceProvider.GetRequiredService<DemoServer>();

try
{
    server.Start(10001);
}
catch (Exception ex)
{
    Logger.Error(ex.ToString());
}
