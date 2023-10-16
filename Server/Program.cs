using Network.TCP;

TCPListener tcpListener = new TCPListener();
tcpListener.Listen("0.0.0.0", 10001);

Console.ReadKey();