using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

using ShootyShootyRL.Mapping;

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
    public class AICreature:Creature
    {
        [field:NonSerialized()]
        AI _ai;

        public AI AI
        {
            get
            {
                return _ai;
            }
        }

        static int AI_FREQUENCY = 5;
        int rounds_since_ai = 5;

        public override bool Save()
        {
            initialized = false;
            return true;
        }

        public new void Tick()
        {
            base.Tick();
            if (rounds_since_ai >= AI_FREQUENCY)
            {
                _ai.Tick();
                rounds_since_ai = -1;
            }
            rounds_since_ai += 1;
        }

        public void Init(TCODColor color, MessageHandler msg, Faction fac, Action firstAction, AI ai, Map m)
        {
            base.Init(color, msg, fac, firstAction);
            _ai = ai;
            _ai.Init(this, m);
        }

        public AICreature(int x, int y, int z, String name, String desc, char displaychar, Bodies.Body body):base(x,y,z,name,desc, displaychar, body)
        {
        }
    }
}
