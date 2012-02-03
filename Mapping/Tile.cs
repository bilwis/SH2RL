using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

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
