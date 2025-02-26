using Shared;
using Shared.Common;
using Shared.Logger;
using Shared.Network;

namespace SnakeBattleServer;

public class Room
{
    public string KeyToEnterRoom { get; private set; }

    public bool IsBattling { get; private set; }
    private bool _hasStarted;

    private BattleClient _player1Client { get; set; }
    private BattleClient _player2Client { get; set; }

    private SnakeUnit _player1Snake => _player1Client.SnakeUnit;
    private SnakeUnit _player2Snake => _player2Client.SnakeUnit;

    private Random _random;
    private int _minX = -8650, _maxX = 8650;
    private int _minY = -4800, _maxY = 4800;
    private int _foodX, _foodY;

    private BattleServer _battleServer;

    public Room(string keyToEnterRoom, BattleServer battleServer)
    {
        _battleServer = battleServer;
        KeyToEnterRoom = keyToEnterRoom;
        _random = new Random();
    }

    public void SetPlayerClient(BattleClient client)
    {
        if (_player1Client == null)
        {
            _player1Client = client;
        }
        else if (_player2Client == null)
        {
            _player2Client = client;
        }
        else
        {
            Log.Error("重複設定Player Client");
        }
    }

    public void FixedUpdate()
    {
        if (!IsTwoPlayerInRoom())
            return;

        if (!IsBattling && !_hasStarted)
        {
            StartBattle();
            _hasStarted = true;
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

        SpawnFood();
        
        _player1Client.SnakeUnit = new SnakeUnit(-5000, 1000, 0);
        _player2Client.SnakeUnit = new SnakeUnit(5000, -1000, 180);

        _player1Client.SnakeUnit.AddLength();
        _player1Client.SnakeUnit.AddLength();
        _player1Client.SnakeUnit.AddLength();
        _player1Client.SnakeUnit.AddLength();

        _player2Client.SnakeUnit.AddLength();
        _player2Client.SnakeUnit.AddLength();
        _player2Client.SnakeUnit.AddLength();
        _player2Client.SnakeUnit.AddLength();

        var currentState = ByteBufferPool.Shared.Rent(32);

        currentState.WriteInt32(_player1Snake.Position.X);
        currentState.WriteInt32(_player1Snake.Position.Y);
        currentState.WriteInt32(_player1Snake.Length);
        currentState.WriteInt32(_player2Snake.Position.X);
        currentState.WriteInt32(_player2Snake.Position.Y);
        currentState.WriteInt32(_player2Snake.Length);
        currentState.WriteInt32(_foodX);
        currentState.WriteInt32(_foodY);

        _player1Client.SendMessage((ushort)MessageId.B2C_BattleStart, currentState);
        _player2Client.SendMessage((ushort)MessageId.B2C_BattleStart, currentState);
        ByteBufferPool.Shared.Return(currentState);
    }

    private void UpdateBattle()
    {
        if (!IsBattling)
            return;
        
        _player1Snake.FixedUpdate();
        _player2Snake.FixedUpdate();

        CheckEatFood();
        CheckCollision();

        var currentState = ByteBufferPool.Shared.Rent(32);

        currentState.WriteInt32(_player1Snake.Position.X);
        currentState.WriteInt32(_player1Snake.Position.Y);
        currentState.WriteInt32(_player1Snake.Length);
        currentState.WriteInt32(_player2Snake.Position.X);
        currentState.WriteInt32(_player2Snake.Position.Y);
        currentState.WriteInt32(_player2Snake.Length);
        currentState.WriteInt32(_foodX);
        currentState.WriteInt32(_foodY);

        _player1Client.SendMessage((ushort)MessageId.B2C_SyncState, currentState);
        _player2Client.SendMessage((ushort)MessageId.B2C_SyncState, currentState);
        ByteBufferPool.Shared.Return(currentState);
    }

    private void CheckCollision()
    {
        for (var i = 0; i < _player2Snake.Body.Count; i++)
        {
            var dx = _player1Snake.Position.X - _player2Snake.Body[i].X;
            if(dx > 500)
                continue;
            var dy = _player1Snake.Position.Y - _player2Snake.Body[i].Y;
            if(dy > 500)
                continue;

            if (dx * dx + dy * dy <= 500 * 500)
            {
                EndBattle(BattleEndResult.Success, _player2Client.PlayerId);
            }
        }
        
        for (var i = 0; i < _player1Snake.Body.Count; i++)
        {
            var dx = _player2Snake.Position.X - _player1Snake.Body[i].X;
            if(dx > 500)
                continue;
            var dy = _player2Snake.Position.Y - _player1Snake.Body[i].Y;
            if(dy > 500)
                continue;

            if (dx * dx + dy * dy <= 500 * 500)
            {
                EndBattle(BattleEndResult.Success, _player1Client.PlayerId);
            }
        }
    }

    private void CheckEatFood()
    {
        var dx = _player1Snake.Position.X - _foodX;
        var dy = _player1Snake.Position.Y - _foodY;
        if ((dx * dx + dy * dy) <= 500f * 500f)
        {
            SpawnFood();
            _player1Snake.AddLength();
        }
        
        dx = _player2Snake.Position.X - _foodX;
        dy = _player2Snake.Position.Y - _foodY;
        if ((dx * dx + dy * dy) <= 500f * 500f)
        {
            SpawnFood();
            _player2Snake.AddLength();
        }
    }

    private void SpawnFood()
    {
        _foodX = _random.Next(_minX, _maxX + 1);
        _foodY = _random.Next(_minY, _maxY + 1);
    }

    public void SetPlayerInput(BattleClient client, int input)
    {
        if (client == _player1Client)
            _player1Snake.Input = input;
        else if (client == _player2Client)
            _player2Snake.Input = input;
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

        _battleServer.RemoveRoom(KeyToEnterRoom);
    }
}