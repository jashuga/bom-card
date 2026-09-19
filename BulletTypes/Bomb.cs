using Godot;
using System;
public partial class Bomb : bullet
{
    BulletTypes BulletType = BulletTypes.ULTIMATE;
    public override void _Process(double delta)
    {
        //timer for bomb fuse where it goes off anyways
    }

    public override void OnBulletEntered(Node e)
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