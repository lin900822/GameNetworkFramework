using Server;
using Shared;
using Shared.Common;
using Shared.Logger;
using Shared.Network;
using SnakeMainServer.PO;

namespace SnakeMainServer;

public partial class MainServer
{
    private AwaitLock _registerAwaitLock = new AwaitLock();

    [MessageRoute((ushort)MessageId.C2M_PlayerLoginOrRegister)]
    public async Task C2M_PlayerLoginOrRegister(MainClient client, ByteBuffer request)
    {
        if (!request.TryDecode(out C2M_PlayerLoginOrRegister playerData))
            return;

        if (string.IsNullOrWhiteSpace(playerData.Username) || string.IsNullOrWhiteSpace(playerData.Password))
        {
            client.SendStateCode(StateCode.LoginOrRegister_Failed_InfoEmpty);
            return;
        }
        
        PlayerPO playerPo;

        if (playerData.IsLogin)
        {
            playerPo = await _playerRepository.GetPlayerAsync(playerData.Username);
            if (playerPo == null)
            {
                client.SendStateCode(StateCode.Login_Failed_InfoWrong);
                return;
            }

            if (!playerPo.Password.Equals(playerData.Password))
            {
                client.SendStateCode(StateCode.Login_Failed_InfoWrong);
                return;
            }

            playerPo.LastLoggedIn = DateTime.UtcNow;
            await _playerRepository.Update(playerPo);

            if (_playerIdToMainClient.TryGetValue(playerPo.PlayerId, out MainClient alreadyLoggedInClient))
            {
                alreadyLoggedInClient.SetNotLoggedIn();
                alreadyLoggedInClient.SendStateCode(StateCode.Another_User_LoggedIn);
                _playerIdToMainClient[playerPo.PlayerId] = client;
            }
            else
            {
                _playerIdToMainClient.Add(playerPo.PlayerId, client);
            }

            client.SetDataAfterLoginSuccess(playerPo);

            client.SendStateCode(StateCode.Login_Success);
        }
        else
        {
            using (await _registerAwaitLock.Lock())
            {
                var isUserExist = await _playerRepository.IsUserExist(playerData.Username);
                if (isUserExist)
                {
                    client.SendStateCode(StateCode.Register_Failed_UserExist);
                    return;
                }

                playerPo = new PlayerPO()
                {
                    PlayerId = _nextPlayerId,
                    Username = playerData.Username,
                    Password = playerData.Password,
                    CreatedAt = DateTime.UtcNow,
                    LastLoggedIn = DateTime.UtcNow,
                    Coins = 1000,
                };
                await _playerRepository.Insert(playerPo);

                _playerIdToMainClient.Add(playerPo.PlayerId, client);

                client.SetDataAfterLoginSuccess(playerPo);

                _nextPlayerId++;
            }

            client.SendStateCode(StateCode.Register_Success);
        }
        
        var m2cLoginSync = new M2C_LoginSync()
        {
            PlayerId = playerPo.PlayerId,
            Username = playerPo.Username,
            Coins = playerPo.Coins,
        };
        client.SendMessage((ushort)MessageId.M2C_LoginSync, ProtoUtils.Encode(m2cLoginSync));
    }

    [MessageRoute((ushort)MessageId.Debug)]
    public async Task C2M_Debug(MainClient client, ByteBuffer request)
    {
        if (!request.TryDecode(out C2M_Debug debug))
            return;
        
        Log.Error("Socket To Communicator");
        foreach (var paires in _networkListener.SocketToCommunicators)
        {
            Log.Warn($"{paires.Key.GetHashCode()} -> {paires.Value.GetHashCode()}");
        }

        Log.Error("Communicator To TClient");
        foreach (var paires in CommunicatorToTClient)
        {
            Log.Warn($"{paires.Key.GetHashCode()} -> {paires.Value.GetHashCode()}");
        }
        
        Log.Error("_playerId To MainClient");
        foreach (var paires in _playerIdToMainClient)
        {
            Log.Warn($"{paires.Key.GetHashCode()} -> {paires.Value.GetHashCode()} {paires.Value.Communicator.GetHashCode()}");
        }
    }
}