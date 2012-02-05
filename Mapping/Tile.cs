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
    public class Tile
    {
        private String _guid;
        public String ID
        {
            get
            {
                return _guid;
            }
        }

        public String Name;
        public String Description;
        public TCODColor ForeColor;
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

        public Tile(String Name, String Description, TCODColor ForeColor, TCODColor BackColor, char DisplChar, bool BlocksMovement, bool BlocksLOS)
        {
            _guid = System.Guid.NewGuid().ToString();
            this.Name = Name;
            this.Description = Description;
            this.ForeColor = ForeColor;
            this.BackColor = BackColor;
            this._char = DisplChar;
            this.BlocksLOS = BlocksLOS;
            this.BlocksMovement = BlocksMovement;
        }
        

        public Tile(String ID, String Name, String Description, TCODColor ForeColor, TCODColor BackColor, char DisplChar, bool BlocksMovement, bool BlocksLOS)
        {
            _guid = ID;
            this.Name = Name;
            this.Description = Description;
            this.ForeColor = ForeColor;
            this.BackColor = BackColor;
            this._char = DisplChar;
            this.BlocksLOS = BlocksLOS;
            this.BlocksMovement = BlocksMovement;
        }
    }
}
