using Godot;
using System;
public partial class PiercingBullet : bullet
{
    BulletTypes BulletType = BulletTypes.SECONDARY;
    public override void OnBulletEntered(Node e)
    {
        if (e is Enemy)
        {
            GD.Print("hit");
            //TODO: copy all code from normal bullet besides deleting bullet
        }
    }
}