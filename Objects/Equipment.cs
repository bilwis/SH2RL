using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL.Objects
{
    [Serializable()]
    public enum EquipmentSlot:int
    {
        Head = 0,
        Neck = 1,
        Shoulder = 2,
        Forearm = 3,
        Hands = 4,
        Chest = 5,
        Legs = 6,
        Feet = 7,
        Light =  8,
        Ranged = 9,
        Melee = 10,
        Thrown = 11
    }

    [Serializable()]
    public class Equipment<TItem> where TItem:EquippableItem
    {
        protected Dictionary<int, TItem> eq_dict;

        public TItem this[int index]
        {
            get
            {
                return eq_dict[index];
            }
        }

        /// <summary>
        /// Gets the number of Objects (Key/Value Pairs) in the Equipments internal dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                return eq_dict.Count;
            }
        }

        public Equipment()
        {
            eq_dict = new Dictionary<int, TItem>();
        }

        public virtual bool Equip(int slot, TItem item)
        {
            if (eq_dict.ContainsKey(slot))
                return false;

            item.OnEquip();
            eq_dict.Add(slot, item);
            return true;
        }

        public virtual bool Replace(int slot, TItem item)
        {
            if (eq_dict.ContainsKey(slot))
                Unequip(slot);

            item.OnEquip();
            eq_dict.Add(slot, item);
            return true;
        }

        public virtual EquippableItem Unequip(int slot)
        {
            if (!eq_dict.ContainsKey(slot))
                return null;

            EquippableItem i = eq_dict[slot];
            i.OnUnequip();
            eq_dict.Remove(slot);
            return i;
        }

        public bool TryGetItem(int slot, out TItem value)
        {
            if (SlotFilled(slot))
            {
                value = eq_dict[slot];
                return true;
            }

            value = null;
            return false;
        }

        public bool SlotFilled(int slot)
        {
            return eq_dict.ContainsKey(slot);
        }

        public bool ItemEquipped(TItem item)
        {
            return eq_dict.ContainsValue(item);
        }

        public List<int> Geints()
        {
            return eq_dict.Keys.ToList<int>();
        }

        public List<TItem> GetItems()
        {
            return eq_dict.Values.ToList<TItem>();
        }
    }
}
