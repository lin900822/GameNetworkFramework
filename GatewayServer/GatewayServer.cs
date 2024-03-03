using Core.Common;
using Core.Logger;
using Core.Network;
using Server;
using Shared;

namespace GatewayServer;

public partial class GatewayServer : ServerBase<GatewayClient>
{
    private NetworkAgent _accountAgent;

    public GatewayServer(ServerSettings settings) : base(settings)
    {
        _accountAgent = new NetworkAgent();
        _accountAgent.OnConnected += SendServerInfo;
    }

    ~GatewayServer()
    {
        _accountAgent.OnConnected -= SendServerInfo;
    }

    protected override void OnInit()
    {
        _accountAgent.Connect("127.0.0.1", 10011).Await();
    }

    protected override void OnUpdate()
    {
        _accountAgent.Update();
    }

    protected override void OnFixedUpdate()
    {
        foreach (var communicator in ClientList.Keys)
        {
            var client = ClientList[communicator];
            if (TimeUtils.GetTimeStamp() - client.ConnectedTime >= 5000 && !client.HasLoggedIn)
            {
                //Close(communicator);
            }
        }
    }

    private void SendServerInfo()
    {
        var info = ByteBufferPool.Shared.Rent();
        info.WriteUInt16((ushort)_settings.ServerId);
        _accountAgent.SendMessage((ushort)MessageId.ServerInfo, info);
    }
    
    [MessageRoute((ushort)MessageId.Register)]
    public async Task OnReceiveRegister(GatewayClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var response = await _accountAgent.SendRequest((ushort)MessageId.Register, receivedMessageInfo.Message);

        // var state = response.Message.ReadUInt16();
        // if (state == 1)
        // {
        //     Log.Info($"註冊成功");
        //     client.HasLoggedIn = true;
        // }
        // else
        // {
        //     Log.Info($"註冊失敗");
        // }
        
        client.Send((ushort)MessageId.Register, response.Message);
    }

    [MessageRoute((ushort)MessageId.Login)]
    public async Task OnReceiveLogin(GatewayClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var response = await _accountAgent.SendRequest((ushort)MessageId.Login, receivedMessageInfo.Message);

        client.Send((ushort)MessageId.Login, response.Message);
        
        // var state = response.Message.ReadUInt16();
        // if (state == 1)
        // {
        //     Log.Info($"登入成功");
        //     client.HasLoggedIn = true;
        // }
        // else
        // {
        //     Log.Info($"登入失敗");
        // }
    }
}