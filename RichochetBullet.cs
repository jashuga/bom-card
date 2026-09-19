using Godot;
using System;

public partial class RicochetBullet : bullet
{
    BulletTypes BulletType = BulletTypes.SECONDARY;
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