using Network.TCP;

TCPListener tcpListener = new TCPListener();
tcpListener.Start("0.0.0.0", 10001);

Console.ReadKey();