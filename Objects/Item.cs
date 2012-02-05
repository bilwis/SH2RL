﻿using System;
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
