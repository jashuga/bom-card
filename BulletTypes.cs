/* global using enums;
using Godot;
using System;
using System.Threading.Tasks;



public class DownBullet : bullet
{
    var BulletType = BulletTypes.SECONDARY;
    public override void _Process(double delta)
    {
        //move bullet down until it hits bottom of screen
        Vector2 velocity = new Vector2();
        velocity.Y = 100;
        Position += velocity * (float)delta;

    }
}

public class RightBullet : bullet
{
    BulletType = BulletTypes.SECONDARY;
    public override void _Process(double delta)
    {
        //move bullet right until it hits bottom of screen
        Vector2 velocity = new Vector2();
        velocity.X = 100;
        Position += velocity * (float)delta;

    }
}
public class LeftBullet : bullet
{
    BulletType = BulletTypes.SECONDARY;
    public override void _Process(double delta)
    {
        //move bullet left until it hits bottom of screen
        Vector2 velocity = new Vector2();
        velocity.X = -100;
        Position += velocity * (float)delta;

    }
}












 */