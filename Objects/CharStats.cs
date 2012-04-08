using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL.Objects
{
    [Serializable()]
    public struct CharStats
    {
        /// <summary>
        /// STR governs the Creatures' ability to use melee weapons,
        /// some heavy ranged weapons, armor and certain utility items.
        /// </summary>
        public int Strength;

        /// <summary>
        /// DEX governs the Creatures' ability to use ranged weapons and
        /// evade incoming attacks.
        /// </summary>
        public int Dexterity;

        /// <summary> 
        /// INT enables the Creature to craft items, navigate more efficiently,
        /// collect resources and use certain self-defense technologies.
        /// </summary>
        public int Intelligence;

        public CharStats(int str, int dex, int itl)
        {
            Strength = str;
            Dexterity = dex;
            Intelligence = itl;
        }

        public double GetCarryCapacity()
        {
            return Strength * 3.0d;
        }

    }
}
