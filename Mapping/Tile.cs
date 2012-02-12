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

namespace ShootyShootyRL.Mapping
{
    /// <summary>
    /// This objects represents a type of tile on the map.
    /// </summary>
    [Serializable()]
    public class Tile
    {
        private String _guid;
        public String GUID
        {
            get
            {
                return _guid;
            }
        }

        public String Name;
        public String Description;
        [field:NonSerialized()]
        public TCODColor ForeColor;
        [field:NonSerialized()]
        public TCODColor BackColor;

        char _char;
        public String DisplayString
        {
            get
            {
                return _char.ToString();
            }
        }
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

        public bool BlocksMovement;
        public bool BlocksLOS;

        bool initialized = false;

        public Tile(String Name, String Description, char DisplChar, bool BlocksMovement, bool BlocksLOS)
        {
            _guid = System.Guid.NewGuid().ToString();
            this.Name = Name;
            this.Description = Description;
            this._char = DisplChar;
            this.BlocksLOS = BlocksLOS;
            this.BlocksMovement = BlocksMovement;
        }

        public void Init(TCODColor ForeColor, TCODColor BackColor)
        {
            //NOTE: Why is the color not set in the constructor? Because the "foreign"
            //TCODColor objects can't be serialized, so they are saved seperately
            //and initialized after the constructor.

            this.ForeColor = ForeColor;
            this.BackColor = BackColor;
            initialized = true;
        }

        public bool Save()
        {
            initialized = false;
            return true;
        }

    }
}
