using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL.Objects
{
    [Serializable()]
    public class Inventory<T> where T:Object
    {
        protected Dictionary<String, T> inv_dict;

        public T this[string index]
        {
            get
            {
                return inv_dict[index];
            }
        }

        /// <summary>
        /// Gets the number of Objects (Key/Value Pairs) in the Inventories internal dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                return inv_dict.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return inv_dict.Values.GetEnumerator();
        }

        public Inventory()
        {
            inv_dict = new Dictionary<string, T>();
        }

        public virtual bool Add(T item)
        {
            if (inv_dict.ContainsKey(item.GUID))
                return false;

            inv_dict.Add(item.GUID, item);
            return true;
        }

        public virtual bool Remove(String guid)
        {
            if (!inv_dict.ContainsKey(guid))
                return false;

            inv_dict.Remove(guid);
            return true;
        }

        public bool TryGetValue(String key, out T value)
        {
            if (ContainsKey(key))
            {
                value = inv_dict[key];
                return true;
            }

            value = null;
            return false;
        }

        public bool ContainsKey(String guid)
        {
            return inv_dict.ContainsKey(guid);
        }

        public bool ContainsValue(T item)
        {
            return inv_dict.ContainsValue(item);
        }

        public List<String> GetKeys()
        {
            return inv_dict.Keys.ToList<String>();
        }

        public List<T> GetValues()
        {
            return inv_dict.Values.ToList<T>();
        }

    }

    [Serializable()]
    public class WeightedInventory<T> : Inventory<T> where T : Item
    {
        private double weight;
        private double capacity;

        public WeightedInventory(double capacity=0.0d):
            base()
        {
            this.capacity = capacity;
        }

        public double GetCapacity()
        {
            return capacity;
        }

        public double GetWeight()
        {
            return weight;
        }

        public override bool Add(T item)
        {
            double item_weight = item.Weight;

            if (capacity > 0.0d && (weight + item_weight) < capacity)
            {
                weight += item_weight;
                return base.Add(item);
            }

            return false;
        }

        public override bool Remove(string guid)
        {
            T item;
            if (inv_dict.TryGetValue(guid, out item))
            {
                weight -= item.Weight;
                return base.Remove(guid);
            }

            return false;
        }
    }
}
