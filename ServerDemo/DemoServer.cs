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
    
    public DemoServer(UserRepository userRepository, ServerSettings settings) : base(settings)
    {
        _userRepository = userRepository;
    }
    
    protected override void OnInit()
    {
        var byteBuffer = ByteBufferPool.Shared.Rent(20);
        byteBuffer.WriteUInt32(99);
        byteBuffer.WriteUInt32(99);
        byteBuffer.WriteUInt32(99);
        _cacheRawByteData = new byte[byteBuffer.Length];
        byteBuffer.Read(_cacheRawByteData, 0, byteBuffer.Length);
        
        _userRepository.Init().SafeWait();
    }

    protected override void OnFixedUpdate()
    {
        
    }

    protected override void OnDeinit()
    {
        
    }
}