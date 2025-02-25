using System.Net.Sockets;

namespace SnakeBattleServer;

public struct Vec2
{
    public Vec2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X;
    public int Y;
}

public class SnakeUnit
{
    public Vec2 Position;
    public int Input { get; set; }
    public int Speed { get; set; } = 5;

    public int Length => Body.Count;

    public List<Vec2> Body { get; private set; }

    public SnakeUnit(int startX, int startY, int input)
    {
        Body = new List<Vec2>();
        Position = new Vec2(startX, startY);
        Body.Add(Position);
        Input = input;
    }

    public void AddLength()
    {
        Vec2 tail = Body[^1]; // 取最後一節
        Body.Add(tail); // 在尾巴加一節（等一下更新時會自動調整位置）
    }

    public void FixedUpdate()
    {
        Move();
        DetectBorder();
    }

    private void Move()
    {
        double radians = Input * Math.PI / 180;
        var x = Position.X + ((1000 / 20) * Speed * Math.Cos(radians));
        var y = Position.Y + ((1000 / 20) * Speed * Math.Sin(radians));

        for (int i = Body.Count - 1; i > 0; i--)
        {
            Body[i] = Body[i - 1];
        }
        
        Position = new Vec2((int)x, (int)y);
        Body[0] = Position;
    }

    private void DetectBorder()
    {
        if (Position.X > 8650)
            Position.X = 8650;
        if (Position.X < -8650)
            Position.X = -8650;
        if (Position.Y > 4800)
            Position.Y = 4800;
        if (Position.Y < -4800)
            Position.Y = -4800;
    }
}

