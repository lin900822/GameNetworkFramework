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
    public async Task<Response> OnReceiveUserRegister(AccountClient client, ReceivedMessageInfo receivedMessageInfo)
    {
        if (!receivedMessageInfo.TryDecode<User>(out var user)) return Response.None;
        
        if(user.Username.Length <= 2) return Response.None;

        var isAccountExist = await _accountRepository.IsAccountExist(user.Username);
        if (isAccountExist)
        {
            return Response.Create((uint)StateCode.Register_Failed_UserExist);
        }

        Interlocked.Increment(ref _accountMaxId);
        
        await _accountRepository.Insert(new Account()
        {
            Id       = _accountMaxId,
            Username = user.Username,
            Password = user.Password,
        });
        
        return Response.Create((uint)StateCode.Success);
    }
}