using Godot;
using System;
public partial class HeavyBullet : bullet
{
    BulletTypes BulletType = BulletTypes.SECONDARY;

    public override void _Process(double delta)
    {
        //move bullet up slowly until it hits top of screen
        Vector2 velocity = new Vector2();
        velocity.Y = -70;
        Position += velocity * (float)delta;

    }
    public override void OnBulletEntered(Node e)
    {
        if (e is Enemy)
        {
            GD.Print("hit");
            //TODO: copy all code from normal bullet but do 5 damage
        }
    }
}