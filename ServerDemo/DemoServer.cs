using System.Diagnostics;
using Core.Common;
using Core.Metrics;
using Core.Network;
using Server;
using ServerDemo.Repositories;
using Debugger = Core.Metrics.Debugger;

namespace ServerDemo;

public partial class DemoServer : ServerBase<DemoClient>
{
    private byte[] _cacheRawByteData;
    
    private UserRepository _userRepository;
    //private ClientBase _connectorClient;
    
    public DemoServer(UserRepository userRepository, ServerSettings settings) : base(settings)
    {
        _userRepository = userRepository;

        //_connectorClient = new ClientBase();
    }
    
    protected override void OnInit()
    {
        //InitDebug();
        //_connectorClient.Connect("127.0.0.1", 10002);
        
        var byteBuffer = ByteBufferPool.Shared.Rent(20);
        byteBuffer.WriteUInt32(99);
        byteBuffer.WriteUInt32(99);
        byteBuffer.WriteUInt32(99);
        _cacheRawByteData = new byte[byteBuffer.Length];
        byteBuffer.Read(_cacheRawByteData, 0, byteBuffer.Length);
        
        //_userRepository.Init().SafeWait();
    }

    protected override void OnFixedUpdate()
    {
        //_connectorClient.Update();
    }

    protected override void OnDeinit()
    {
        
    }
}