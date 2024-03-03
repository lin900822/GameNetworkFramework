using AccountServer.Repositories;
using AccountServer.Repositories.Data;
using Core.Common;
using Core.Logger;
using Core.Network;
using Server;
using Shared;

namespace AccountServer;

public class AccountServer : ServerBase<AccountClient>
{
    private Dictionary<ushort, AccountClient> _clientsByServerId = new Dictionary<ushort, AccountClient>();

    private AccountRepository _accountRepository;

    private uint _accountMaxId;

    public AccountServer(AccountRepository accountRepository, ServerSettings settings) : base(settings)
    {
        _accountRepository = accountRepository;
    }

    protected override void OnInit()
    {
        _accountRepository.GetMaxId().Await((result) => { _accountMaxId = result; });
    }

    protected override void OnClientDisconnected(AccountClient client)
    {
        if (_clientsByServerId.Remove((ushort)client.Id))
        {
            Log.Info($"Server({client.Id.ToString()}) disconnected!");
            return;
        }

        Log.Error($"Server({client.Id.ToString()}) does not exist!");
    }

    [MessageRoute((ushort)MessageId.ServerInfo)]
    public void OnReceiveServerInfo(AccountClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var info     = receivedMessageInfo.Message;
        var serverId = info.ReadUInt16();

        if (_clientsByServerId.ContainsKey(serverId))
        {
            Log.Error($"Server({serverId.ToString()}) exist!");
            return;
        }

        client.Id = serverId;
        _clientsByServerId.Add((ushort)client.Id, client);
        Log.Info($"Server({client.Id.ToString()}) connected!");
    }

    [MessageRoute((ushort)MessageId.Register)]
    public async Task<ByteBuffer> OnReceiveRegister(AccountClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var response = ByteBufferPool.Shared.Rent(10);

        if (!receivedMessageInfo.TryDecode<User>(out var user))
        {
            response.WriteUInt16(0);
            return response;
        }

        if (user.Username.Length <= 2)
        {
            response.WriteUInt16(0);
            return response;
        }

        var isAccountExist = await _accountRepository.IsAccountExist(user.Username);
        if (isAccountExist)
        {
            response.WriteUInt16(0);
            return response;
        }

        Interlocked.Increment(ref _accountMaxId);

        await _accountRepository.Insert(new Account()
        {
            Id       = _accountMaxId,
            Username = user.Username,
            Password = user.Password,
        });

        response.WriteUInt16(1);
        return response;
    }

    [MessageRoute((ushort)MessageId.Login)]
    public async Task<ByteBuffer> OnReceiveLogin(AccountClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var response = ByteBufferPool.Shared.Rent(10);

        if (!receivedMessageInfo.TryDecode<User>(out var user))
        {
            response.WriteUInt16(0);
            return response;
        }

        if (user.Username.Length <= 2)
        {
            response.WriteUInt16(0);
            return response;
        }

        var userPo = await _accountRepository.GetAccountAsync(user.Username);
        if (userPo != null)
        {
            if (userPo.Password.Equals(user.Password))
            {
                response.WriteUInt16(1);
            }
            else
            {
                response.WriteUInt16(0);
            }
        }
        else
        {
            response.WriteUInt16(0);
        }

        return response;
    }
}