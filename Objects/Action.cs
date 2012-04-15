using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    /// <summary>
    /// Represents a type of action, e.g. Move, Attack etc.
    /// </summary>
    public enum ActionType
    {
        Move = 0,
        Attack = 1,
        Swap = 2,
        Interact = 3,
        Drop = 4,
        Take = 5,
        Idle = 6,
        Equip = 7,
        Unequip = 8
    }

    /// <summary>
    /// This class represents an action that is scheduled for a creature.
    /// </summary>
    public class Action
    {
        public ActionType Type;
        public Creature Subject;
        public ActionParameters Param;
        public double Cost;


        /// <summary>
        /// Creates a new action for the given creature with the given parameters and cost.
        /// </summary>
        /// <param name="type">An ActionType enumeration entry describing the type of the action.</param>
        /// <param name="param">An ActionParameters object.</param>
        /// <param name="subject">The subject, which actually performs the action.</param>
        /// <param name="cost">The cost of the action.</param>
        public Action(ActionType type, ActionParameters param, Creature subject, double cost)
        {
            this.Type = type;
            this.Subject = subject;
            this.Cost = cost;

            //Check if correct parameters
            switch (Type)
            {
                case ActionType.Move:
                    if (param.GetType() != typeof(MovementActionParameters))
                        throw new Exception("Tried to construct Action of type MOVE, but parameters are not MovementActionParameters!");
                    break;
                case ActionType.Take:
                    if (param.GetType() != typeof(TakeActionParameters))
                        throw new Exception("Tried to construct Action of type TAKE, but parameters are not TakeActionParameters!");
                    break;
                case ActionType.Equip:
                    if (param.GetType() != typeof(EquipActionParameters))
                        throw new Exception("Tried to construct Action of type EQUIP, but parameters are not EquipActionParameters!");
                    break;
                case ActionType.Unequip:
                    if (param.GetType() != typeof(UnequipActionParameters))
                        throw new Exception("Tried to construct Action of type UNEQUIP, but parameters are not UnequipActionParameters!");
                    break;
            }

            this.Param = param;
        }

    }

    public abstract class ActionParameters
    { }

    /// <summary>
    /// This class holds all necessary parameters for a "Move" action, i.e. the target of the move and it's map.
    /// </summary>
    public class MovementActionParameters:ActionParameters
    {
        public int Target_X;
        public int Target_Y;
        public int Target_Z;
        public Map map;

        public MovementActionParameters(int abs_x, int abs_y, int abs_z, Map m)
        {
            Target_X = abs_x;
            Target_Y = abs_y;
            Target_Z = abs_z;
            map = m;
        }
    }

    public class TakeActionParameters : ActionParameters
    {
        public Map map;
        public String guid;

        public TakeActionParameters(String guid, Map m)
        {
            this.guid = guid;
            map = m;
        }
    }

    public class EquipActionParameters : ActionParameters
    {
        public String guid;

        public EquipActionParameters(String guid)
        {
            this.guid = guid;
        }
    }

    public class UnequipActionParameters : ActionParameters
    {
        public EquipmentSlot slot;

        public UnequipActionParameters(EquipmentSlot slot)
        {
            this.slot = slot;
        }
    }
}
