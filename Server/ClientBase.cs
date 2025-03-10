﻿using Shared.Network;

namespace Server;

public abstract class ClientBase<T> where T : ClientBase<T>, new()
{
    public ServerBase<T> Server { get; private set; }

    public NetworkCommunicator Communicator => _communicator;
    private NetworkCommunicator _communicator;

    public long LastPingTime { get; set; }

    #region - Life Cycle -

    public void Init(ServerBase<T> server, NetworkCommunicator communicator)
    {
        _communicator = communicator;
        Server        = server;
        
        OnInit();
    }

    public void FixedUpdate()
    {
        OnFixedUpdate();
    }

    public void Deinit()
    {
        OnDeinit();
    }

    protected virtual void OnInit()
    {
    }

    protected virtual void OnFixedUpdate()
    {
    }

    protected virtual void OnDeinit()
    {
    }

    #endregion
    
    public void SendMessage(ushort messageId, byte[] message, bool isRequest = false, ushort requestId = 0)
    {
        _communicator.Send(messageId, message, isRequest, requestId);
    }
    
    public void SendMessage(ushort messageId, ByteBuffer message, bool isRequest = false, ushort requestId = 0)
    {
        _communicator.Send(messageId, message, isRequest, requestId);
    }
}