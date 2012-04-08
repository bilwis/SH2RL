using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL.Objects
{
    [Serializable()]
    public enum GunType
    {
        Pistol = 0,
        Revolver = 1,
        Rifle = 2,
        Smoothbore = 3,
        Heavy = 4
    }

    [Serializable()]
    public class Firearm:Item
    {
        public Caliber caliber;
        public GunType type;
        public Magazine mag;

        //attachments, weapon xp, ...

        public Firearm(int x, int y, int z, String name, String desc, char displaychar, double weight, Caliber cal, GunType type):
            base(x,y,z,name,desc,displaychar, weight)
        {
            caliber = cal;
            this.type = type;

        }

        public void Reload(Magazine mag)
        {
            this.mag = mag;
        }

    }
}
