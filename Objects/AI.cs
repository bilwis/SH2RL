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
    public enum AIState
    {
        Idling = 0,
        Sleeping = 1,
        Investigating = 2,
        Attacking = 3,
        Fleeing = 4,
        Uninitialized = 5
    }

    [Serializable()]
    public enum AIEvalResult
    {
        Neutral = 0,
        Friendly = 1,
        Enemy = 2,
        Unknown = 3,
        Self = 4
    }

    [Serializable()]
    public abstract class AI
    {
        public String GUID;

        [field:NonSerialized()]
        protected Creature subject;
        [field: NonSerialized()]
        protected Map map;

        protected AIState state;

        protected bool initialized = false;

        protected abstract void observe();
        public abstract void Tick();
        public abstract void Save();
        public abstract void Init(AICreature c, Map m);
        protected abstract int[] getPathToTarget();

    }

    [Serializable()]
    public class WalkerAI : AI
    {
        private double visionRange = 5.0d;
        private string targetID;
        Random rand;

        public override void Tick()
        {
            if (!initialized)
                return;

            observe();

            //TODO: Stick non-flyers to the ground (map.getGround(int x, int y))!

            if (state == AIState.Idling)
            {
                //TODO: ACTION SYSTEM 
                subject.ForeColor = TCODColor.green;
                
                subject.Move(subject.X + rand.Next(-1, 2), subject.Y + rand.Next(-1, 2), subject.Z, map, true);
            }

            if (state == AIState.Attacking)
            {
                Creature tar = map.CreatureList[targetID];
                TCODLine.init(subject.X, subject.Y, tar.X, tar.Y);
                int nx = 0, ny = 0, nz = 0;
                TCODLine.step(ref nx, ref ny);

                if (nx == 0 || ny == 0)
                    return;
                nz = map.DropObject(nx, ny, nz);
                if (nz == -1)
                    return;
                    
                subject.ForeColor = TCODColor.red;
                //subject.Move(subject.X + rand.Next(-1, 2), subject.Y + rand.Next(-1, 2), subject.Z, map);
                subject.Move(nx, ny, nz, map, true);
            }
        }

        public override void Save()
        {
            initialized = false;
        }

        public override void Init(AICreature c, Map m)
        {
            subject = c;
            this.map = m;
            initialized = true;
        }

        protected override void observe()
        {
            double distanceToTarget = 0;

            if (targetID != null) //Creature has target
            {
                Creature tar;
                bool exists = map.CreatureList.TryGetValue(targetID, out tar);
                if (!exists)
                {
                    targetID = null;
                }
                else
                {
                    distanceToTarget = Util.CalculateDistance(tar, subject);
                }

                //Program.game.Out.SendDebugMessage(subject.Name + " is currently " + distanceToTarget + " away from its target.");
            }

            if (targetID == null || distanceToTarget > visionRange)
            {
                targetID = null;
                switchState(AIState.Idling);

                foreach (KeyValuePair<string, Creature> kv in map.CreatureList)
                {
                    if (Util.CalculateDistance(subject, kv.Value) <= visionRange)
                    {
                        if (eval(kv.Value) == AIEvalResult.Enemy)
                        {
                            targetID = kv.Key;
                            switchState(AIState.Attacking);
                            Program.game.Out.SendMessage(subject.Name + " has targeted " + kv.Value.Name + ".");
                        }
                    }
                }
            }
        }

        protected override int[] getPathToTarget()
        {
            throw new NotImplementedException();
        }

        private void switchState(AIState state)
        {
            switch (state)
            {
                case AIState.Sleeping:
                    Program.game.Out.SendMessage("The " + subject.Name + " falls asleep.");
                    break;
                case AIState.Idling:
                    break;
                case AIState.Attacking:
                    break;
                case AIState.Fleeing:
                    break;
                case AIState.Investigating:
                    break;
            }
            this.state = state;
        }

        private AIEvalResult eval(Creature c)
        {
            if (c.GUID == subject.GUID)
                return AIEvalResult.Self;

            FactionRelation fr = subject.Faction.GetRelation(c.Faction);

            if (fr == FactionRelation.Hostile)
                return AIEvalResult.Enemy;

            if (fr == FactionRelation.Friendly)
                return AIEvalResult.Friendly;

            if (fr == FactionRelation.Neutral)
                return AIEvalResult.Neutral;

            return AIEvalResult.Unknown;
        }


        public WalkerAI(int rand_seed)
        {
            GUID = System.Guid.NewGuid().ToString();
            this.state = AIState.Idling;
            rand = new Random(rand_seed);
        }
    }
}
