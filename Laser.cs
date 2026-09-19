using Godot;
using System;
public partial class Laser : bullet
{
    BulletTypes BulletType = BulletTypes.ULTIMATE;
    public override void _Process(double delta)
    {
        //timer that the laser lasts for
    }

    public override void OnBulletEntered(Node e)
    {
        if (e is Enemy)
        {
            GD.Print("hit");
            //copy all code for normal bullet minus bullet deletion
        }
    }
}