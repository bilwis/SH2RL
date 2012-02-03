using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using libtcod;

namespace ShootyShootyRL.Objects
{
    [Serializable()]
    public class Item:Object
    {
        
        public override void SetPosition(int x, int y, int z)
        {
            throw new NotImplementedException();
        }

        public override bool Save()
        {
            initialized = false;
            return true;
        }

        public bool Tick()
        {
            Random rand = new Random();

            if (!initialized)
                return false;

            //this.ForeColor = new TCODColor(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255));

            return true;
        }
        
        public Item(int x, int y, int z, String name, String desc, char displaychar)
        {
            _guid = System.Guid.NewGuid().ToString();
            this.x = x;
            this.y = y;
            this.z = z;
            _name = name;
            _desc = desc;
            
            _char = displaychar;
        }
    }
}
