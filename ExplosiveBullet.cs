using Godot;
using System;
public partial class ExpolsiveBullet : bullet
{
    BulletTypes BulletType = BulletTypes.ULTIMATE;

   
    public override void OnBulletEntered(Node e)
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