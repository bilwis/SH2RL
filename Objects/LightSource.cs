using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using libtcod;

namespace ShootyShootyRL.Objects
{
    [Serializable()]
    public class LightSource:Item
    {
        private byte level;
        private bool recalc;
        private bool active;

        private int prev_x, prev_y, prev_z;
        private byte prev_level;

        public int PrevX
        {
            get
            {
                return prev_x;
            }
        }
        public int PrevY
        {
            get
            {
                return prev_y;
            }
        }
        public int PrevZ
        {
            get
            {
                return prev_z;
            }
        }

        public byte LightLevel
        {
            get
            {
                if (active)
                    return level;
                else
                    return 0;
            }
        }

        public byte PreviousLightLevel
        {
            get
            {
                return prev_level;
            }
        }

        public bool DoRecalculate
        {
            get
            {
                return recalc;
            }
        }

        public LightSource(int x, int y, int z, byte level, string name, string desc, char displ_char):
            base(x,y,z,name, desc, displ_char)
        {
            this.x = x;
            this.y = y;
            this.z = z;

            prev_x = x;
            prev_y = y;
            prev_z = z;

            this.level = level;
            prev_level = level;

            active = false;
            recalc = false;
        }

        public override void SetPosition(int abs_x, int abs_y, int abs_z)
        {
            prev_x = x;
            prev_y = y;
            prev_z = z;

            this.x = abs_x;
            this.y = abs_y;
            this.z = abs_z;

            if (active)
                recalc = true;
        }

        public override bool Init(libtcod.TCODColor fore, MessageHandler messageHandler)
        {
            if (initialized)
                return false;

            ForeColor = fore;
            _messageHandler = messageHandler;
            Activate();

            _visible = true;
            initialized = true;
            return true;
        }

        public override bool Save()
        {
            Deactivate();

            return base.Save();
        }

        public override bool Tick()
        {
            //Random rand = new Random();

            if (!initialized)
                return false;

            //this.ForeColor = new TCODColor(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255));

            return true;
        }

        public void SetRecalculated(bool value)
        {
            prev_level = level;
            prev_x = x;
            prev_y = y;
            prev_z = z;
            recalc = value;
        }

        public void SetRecalculated()
        {
            prev_level = level;
            prev_x = x;
            prev_y = y;
            prev_z = z;
            recalc = false;
        }

        public void SetLevel(byte level)
        {
            //TODO: Handle properly in map

            prev_level = this.level;
            this.level = level;
            if (active)
                recalc = true;
        }

        public void Activate()
        {
            active = true;
            recalc = true;
        }

        public void Deactivate()
        {
            active = false;
            recalc = true;
        }



    }
}
