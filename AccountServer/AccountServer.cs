using AccountServer.Repositories;
using AccountServer.Repositories.Data;
using Core.Logger;
using Core.Network;
using Protocol;
using Server;

namespace AccountServer;

public class AccountServer : ServerBase<AccountClient>
{
    private AccountRepository _accountRepository;

    private uint _accountMaxId;

    public AccountServer(AccountRepository accountRepository, ServerSettings settings) : base(settings)
    {
        _accountRepository = accountRepository;
    }

    protected override async void OnInit()
    {
        _accountMaxId = await _accountRepository.GetMaxId();
    }

    [MessageRoute((ushort)MessageId.Register)]
    public async Task<ByteBuffer> OnReceiveUserRegister(AccountClient client, ReceivedMessageInfo receivedMessageInfo)
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
}