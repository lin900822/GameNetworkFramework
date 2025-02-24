using Shared;
using Shared.Logger;
using Shared.Network;

namespace SnakeBattleServer;

public class Room
{
    private struct Vec2
    {
        public int X;
        public int Y;
    }

    public string KeyToEnterRoom { get; private set; }
    public uint Player1Id { get; private set; }
    public uint Player2Id { get; private set; }

    public bool IsBattling { get; private set; }

    private BattleClient _player1Client { get; set; }
    private BattleClient _player2Client { get; set; }

    private Vec2 _player1Vec2;
    private Vec2 _player2Vec2;
    private int _player1Input;
    private int _player2Input;

    private int _unitSpeed = 5;

    public Room(string keyToEnterRoom, uint player1Id, uint player2Id)
    {
        KeyToEnterRoom = keyToEnterRoom;
        Player1Id = player1Id;
        Player2Id = player2Id;
    }

    public void SetPlayerClient(BattleClient client)
    {
        if (_player1Client == null)
            _player1Client = client;
        else if (_player2Client == null)
            _player2Client = client;
        else
        {
            Log.Error("重複設定Player Client");
        }
    }

    public void FixedUpdate()
    {
        if (!IsTwoPlayerInRoom())
            return;

        if (!IsBattling)
        {
            StartBattle();
            return;
        }

        UpdateBattle();
    }

    private bool IsTwoPlayerInRoom()
    {
        return _player1Client != null && _player2Client != null;
    }

    private void StartBattle()
    {
        Log.Info($"Room {KeyToEnterRoom} 開始遊戲");
        IsBattling = true;

        var currentState = ByteBufferPool.Shared.Rent(16);
        currentState.WriteInt32(_player1Vec2.X);
        currentState.WriteInt32(_player1Vec2.Y);
        currentState.WriteInt32(_player2Vec2.X);
        currentState.WriteInt32(_player2Vec2.Y);
        _player1Client.SendMessage((ushort)MessageId.B2C_BattleStart, currentState);
        _player2Client.SendMessage((ushort)MessageId.B2C_BattleStart, currentState);
        ByteBufferPool.Shared.Return(currentState);
    }

    private void UpdateBattle()
    {
        ConsumeInput();
        DetectBorder();

        var currentState = ByteBufferPool.Shared.Rent(16);
        currentState.WriteInt32(_player1Vec2.X);
        currentState.WriteInt32(_player1Vec2.Y);
        currentState.WriteInt32(_player2Vec2.X);
        currentState.WriteInt32(_player2Vec2.Y);
        _player1Client.SendMessage((ushort)MessageId.B2C_SyncState, currentState);
        _player2Client.SendMessage((ushort)MessageId.B2C_SyncState, currentState);
        ByteBufferPool.Shared.Return(currentState);
    }
    
    public void SetPlayerInput(BattleClient client, int input)
    {
        if (client == _player1Client)
            _player1Input = input;
        else if (client == _player2Client)
            _player2Input = input;
    }

    private void ConsumeInput()
    {
        double radians = _player1Input * Math.PI / 180;
        var x = _player1Vec2.X + ((1000 / 20) * _unitSpeed * Math.Cos(radians));
        var y = _player1Vec2.Y + ((1000 / 20) * _unitSpeed * Math.Sin(radians));
        _player1Vec2.X = (int)(x);
        _player1Vec2.Y = (int)(y);

        radians = _player2Input * Math.PI / 180;
        x = _player2Vec2.X + ((1000 / 20) * _unitSpeed * Math.Cos(radians));
        y = _player2Vec2.Y + ((1000 / 20) * _unitSpeed * Math.Sin(radians));
        _player2Vec2.X = (int)(x);
        _player2Vec2.Y = (int)(y);
    }

    private void DetectBorder()
    {
        if (_player1Vec2.X > 8650)
            _player1Vec2.X = 8650;
        if (_player1Vec2.X < -8650)
            _player1Vec2.X = -8650;
        if (_player1Vec2.Y > 4800)
            _player1Vec2.Y = 4800;
        if (_player1Vec2.Y < -4800)
            _player1Vec2.Y = -4800;
        
        if (_player2Vec2.X > 8650)
            _player2Vec2.X = 8650;
        if (_player2Vec2.X < -8650)
            _player2Vec2.X = -8650;
        if (_player2Vec2.Y > 4800)
            _player2Vec2.Y = 4800;
        if (_player2Vec2.Y < -4800)
            _player2Vec2.Y = -4800;
    }

    public void ClientDisconnected(BattleClient client)
    {
        if (client == _player1Client)
            _player1Client = null;
        else if (client == _player2Client)
            _player2Client = null;
    }

    public void EndBattle(BattleEndResult result, uint winnerPlayerId)
    {
        IsBattling = false;
        
        var endData = ByteBufferPool.Shared.Rent(8);
        endData.WriteInt32((int)result);
        endData.WriteUInt32(winnerPlayerId);
        _player1Client?.SendMessage((ushort)MessageId.B2C_BattleEnd, endData);
        _player2Client?.SendMessage((ushort)MessageId.B2C_BattleEnd, endData);
        ByteBufferPool.Shared.Return(endData);
    }
}