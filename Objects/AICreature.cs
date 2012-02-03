using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

using ShootyShootyRL.Mapping;

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

        public AICreature(int x, int y, int z, String name, String desc, char displaychar):base(x,y,z,name,desc, displaychar)
        {
        }
    }
}
