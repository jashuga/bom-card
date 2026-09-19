global using enums;
using Godot;
using System;
using System.Threading.Tasks;

public class RicochetBullet : bullet
{
    BulletType = BulletTypes.SECONDARY;
    private bool bounced = false;
    public override void _Process(double delta)
    {
        //move bullet up until it hits top of screen
        
        if (!bounced)
        {
            Vector2 velocity = new Vector2();
            velocity.Y = -100;
            Position += velocity * (float)delta;
            if (Position.Y < 0) { bounced = true; }
        } else
        {
            Vector2 velocity = new Vector2();
            velocity.Y = 100;
            Position += velocity * (float)delta;
        }
        
    }
}

public class DownBullet : bullet
{
    BulletType = BulletTypes.SECONDARY;
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

public class PiercingBullet : bullet
{
    BulletType = BulletTypes.SECONDARY;
    private override void OnBulletEntered(Node e)
    {
        if (e is Enemy)
        {
            GD.Print("hit");
            //TODO: copy all code from normal bullet besides deleting bullet
        }
    }
}

public class StrongBullet : bullet
{
    BulletType = BulletTypes.SECONDARY;
    private override void OnBulletEntered(Node e)
    {
        if (e is Enemy)
        {
            GD.Print("hit");
            //TODO: copy all code from normal bullet but do 4 damage
        }
    }
}

public class HeavyBullet : bullet
{
    BulletType = BulletTypes.SECONDARY;

    public override void _Process(double delta)
    {
        //move bullet up slowly until it hits top of screen
        Vector2 velocity = new Vector2();
        velocity.Y = -70;
        Position += velocity * (float)delta;

    }
    private override void OnBulletEntered(Node e)
    {
        if (e is Enemy)
        {
            GD.Print("hit");
            //TODO: copy all code from normal bullet but do 5 damage
        }
    }
}

public class DoubleShot : bullet
{
    //todo spawn another normal bullet after this one is fired
}

public class ExpolsiveBullet : bullet
{
    BulletType = BulletTypes.ULTIMATE;

   
    private override void OnBulletEntered(Node e)
    {
        if (e is Enemy)
        {
            GD.Print("hit");
            //TODO explosion
            //for enemies in a radius:
            //copy all code for normal bullet minus bulelt deletion
        }
    }
}

public class Laser : bullet
{
    BulletType = BulletTypes.ULTIMATE;
    public override void _Process(double delta)
    {
        //timer that the laser lasts for
    }

    private override void OnBulletEntered(Node e)
    {
        if (e is Enemy)
        {
            GD.Print("hit");
            //copy all code for normal bullet minus bullet deletion
        }
    }
}

public class Bomb : bullet
{
    BulletType = BulletTypes.ULTIMATE;
    public override void _Process(double delta)
    {
        //timer for bomb fuse where it goes off anyways
    }

    private override void OnBulletEntered(Node e)
    {
        if (e is Enemy)
        {
            GD.Print("hit");
            //TODO explosion
            //for enemies in a big radius
            //copy all code for normal bullet minus bullet deletion
        }
    }
}