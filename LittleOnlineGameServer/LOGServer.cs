using LittleOnlineGameServer.Repositories;
using LittleOnlineGameServer.Repositories.Data;
using Server;
using Shared;
using Shared.Common;
using Shared.Logger;
using Shared.Network;

namespace LittleOnlineGameServer;

public class LOGServer : ServerBase<LOGClient>
{
    private AccountRepository _accountRepository;
    
    private uint _accountMaxId;
    
    public LOGServer(AccountRepository accountRepository, ServerSettings settings) : base(settings)
    {
        _accountRepository = accountRepository;
    }
    
    protected override void OnInit()
    {
        _accountRepository.Init().SafeWait();
        
        _accountRepository.GetMaxId().Await((result) => { _accountMaxId = result; });
    }
    
    [MessageRoute((ushort)MessageId.Register)]
    public async Task OnReceiveRegister(LOGClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var response = ByteBufferPool.Shared.Rent(10);

        if (!receivedMessageInfo.TryDecode<User>(out var user))
        {
            response.WriteUInt16(0);
            client.SendMessage((ushort)MessageId.Register, response);
            return;
        }

        if (user.Username.Length <= 2)
        {
            response.WriteUInt16(0);
            client.SendMessage((ushort)MessageId.Register, response);
            return;
        }

        var isAccountExist = await _accountRepository.IsAccountExist(user.Username);
        if (isAccountExist)
        {
            response.WriteUInt16(0);
            client.SendMessage((ushort)MessageId.Register, response);
            return;
        }

        Interlocked.Increment(ref _accountMaxId);

        var result = await _accountRepository.Insert(new AccountPO()
        {
            Id       = _accountMaxId,
            Username = user.Username,
            Password = user.Password,
        });

        if (result == 1)
        {
            response.WriteUInt16(1);
        }
        else
        {
            response.WriteUInt16(0);
        }
        
        Log.Info($"Register Success! Affected rows:{result}");
        
        client.SendMessage((ushort)MessageId.Register, response);
    }

    [MessageRoute((ushort)MessageId.Login)]
    public async Task OnReceiveLogin(LOGClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        var response = ByteBufferPool.Shared.Rent(10);

        if (!receivedMessageInfo.TryDecode<User>(out var user))
        {
            response.WriteUInt16(0);
            client.SendMessage((ushort)MessageId.Login, response);
            return;
        }

        if (user.Username.Length <= 2)
        {
            response.WriteUInt16(0);
            client.SendMessage((ushort)MessageId.Login, response);
            return;
        }

        var accountPo = await _accountRepository.GetAccountAsync(user.Username);
        if (accountPo != null)
        {
            if (accountPo.Password.Equals(user.Password))
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

        client.SendMessage((ushort)MessageId.Login, response);
    }
}