using System.Timers;
using Log;
using Microsoft.Extensions.DependencyInjection;
using Network;
using Server;
using Server.Database;
using Server.PO;
using Server.Repositories;
using Timer = System.Timers.Timer;

var connectString = @"server=localhost;port=3306;database=gameserver;SslMode=None;uid=root;pwd=root;Allow User Variables=true";

var serviceCollection = new ServiceCollection();

serviceCollection.AddSingleton<DemoServer>();

serviceCollection.AddSingleton<IDbContext>(new MySqlDbContext(connectString));
serviceCollection.AddSingleton<UserRepository>();

var serviceProvider = serviceCollection.BuildServiceProvider();

var userRepository = serviceProvider.GetRequiredService<UserRepository>();
var list           = await userRepository.SelectAll();
foreach (var item in list)
{
    Logger.Info(item.Username);
}

// var server = serviceProvider.GetRequiredService<DemoServer>();
//
// try
// {
//     server.Start(10001);
// }
// catch (Exception ex)
// {
//     Logger.Error(ex.ToString());
// }
