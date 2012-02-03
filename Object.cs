using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

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
