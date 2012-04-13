using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL.Objects
{
    [Serializable()]
    public class Magazine:Item
    {
        public int Capacity;
        public int Count;
        public Caliber Caliber;
        public AmmoModifier Modifier;

        public bool Eqipped = false;

        public bool TakeProjectiles(int count)
        {
            if (count >= Count)
            {
                Count -= count;
                return true;
            }
            else
                return false;
        }

        //fill up... 

        public Magazine(int x, int y, int z, String name, String desc, char displaychar, double weight, int cap, int count, Caliber cal, AmmoModifier mod):
            base(x,y,z,name,desc,displaychar, weight)
        {
            Capacity = cap;
            Count = count;
            Caliber = cal;
            Modifier = mod;
        }
    }

    [Serializable()]
    public struct Caliber
    {
        public double Diameter;
        public double Length;

        public Caliber(double diam, double len)
        {
            Diameter = diam;
            Length = len;
        }

        public String ToString()
        {
            return "" + Diameter + "x" + Length + "mm";
        }

        public static bool operator ==(Caliber c1, Caliber c2)
        {
            if (c1.Diameter == c2.Diameter && c1.Length == c2.Length)
                return true;

            return false;
        }

        public static bool operator !=(Caliber c1, Caliber c2)
        {
            if (c1.Diameter == c2.Diameter && c1.Length == c2.Length)
                return false;

            return true;
        }
    }

    [Serializable()]
    public enum AmmoModifier
    {
        Regular = 0,
        FMJ = 1,
        HP = 2,
        Inciendiary = 3,
        Tracer = 4
    }
}
