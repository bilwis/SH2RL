using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

//************************************************************//
//*                SHOOTY SHOOTY ROGUELIKE                   *//
//*     some really early pre-alpha version or something     *//
//*     github.com/bilwis/sh2rl       sh2rl.blogspot.com     *//
//*                                                          *//       
//*contains SDL 1.2.15 (GNU LGPL), libtcod 1.5.1 (BSD), zlib *//
//*         1.2.6 (zlib license), SQLite 3.7.10 and          *//
//*     System.Data.SQLite 1.0.79.0, both public domain      *//
//*                                                          *//
//* Please don't copy my stellar source code without asking, *//
//*  but feel free to bask in its glory and draw delicious   *//
//*   inspiration and great knowledge from it! Thank you!    *//
//*                                                          *//
//* bilwis | Clemens Curio                                   *//
//************************************************************//

namespace ShootyShootyRL
{
    [Serializable()]
    public abstract class Object
    {
        protected bool initialized = false;

        protected String _guid;

        public String GUID
        {
            get
            {
                return _guid.ToString();
            }
        }

        protected int x; 
        protected int y; 
        protected int z;

        public int X
        {
            get
            {
                return x;
            }
        }

        public int Y
        {
            get
            {
                return y;
            }
        }

        public int Z
        {
            get
            {
                return z;
            }
        }

        protected char _char;

        public char DisplayChar
        {
            get
            {
                return _char;
            }

            set
            {
                _char = value;
            }
        }

        public string DisplayString
        {
            get
            {
                return _char.ToString();
            }

        }

        [field:NonSerialized()]
        public TCODColor ForeColor;
        [field:NonSerialized()]
        protected MessageHandler _messageHandler;

        public MessageHandler MessageHandler
        {
            get
            {
                return _messageHandler;
            }
        }

        protected String _name;
        protected String _desc;

        public String Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        public String Description
        {
            get
            {
                return _desc;
            }

            set
            {
                _desc = value;
            }
        }

        public abstract bool Save();

        public virtual bool Init(TCODColor fore, MessageHandler messageHandler)
        {
            if (initialized)
                return false;

            ForeColor = fore;
            _messageHandler = messageHandler;

            initialized = true;
            return true;
        }

        public abstract void SetPosition(int x, int y, int z);
    }
}
