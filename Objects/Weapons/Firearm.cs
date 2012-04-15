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
    public enum FireMode
    {
        Semi = 0,
        Burst2 = 1,
        Burst3 = 2,
        FullAuto = 3
    }

    [Serializable()]
    public class Firearm:EquippableItem
    {
        public Caliber caliber;
        public GunType type;
        public Magazine mag;
        public int MagazineCapacity;
        public FireMode mode;
        public List<FireMode> modes;

        //attachments, weapon xp, ...

        public Firearm(int x, int y, int z, String name, String desc, char displaychar, double weight, Caliber cal, GunType type, List<FireMode> modes, int mag_cap):
            base(x,y,z,name,desc,displaychar, weight, EquipmentSlot.Ranged)
        {
            caliber = cal;
            this.type = type;
            this.modes = new List<FireMode>(modes);
            this.mode = modes[0];

            MagazineCapacity = mag_cap;
        }

        public bool Reload(Magazine mag)
        {
            if (mag.Caliber == caliber && mag.Capacity <= MagazineCapacity)
            {
                this.mag = mag;
                return true;
            }

            return false;
        }

        public override void OnEquip()
        {
            this.equipped = true;
        }

        public override void OnUnequip()
        {
            this.equipped = false;
        }

    }
}
