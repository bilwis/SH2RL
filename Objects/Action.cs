using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ShootyShootyRL.Mapping;

namespace ShootyShootyRL.Objects
{
    public enum ActionType
    {
        Move = 0,
        Attack = 1,
        Swap = 2,
        Interact = 3,
        Drop = 4,
        Take = 5,
        Idle = 6
    }

    public class Action
    {
        public ActionType Type;
        public Creature Subject;
        public ActionParameters Param;
        public double Cost;

        public Action(ActionType type, ActionParameters param, Creature subject, double cost)
        {
            this.Type = type;
            this.Subject = subject;
            this.Cost = cost;

            switch (Type)
            {
                case ActionType.Move:
                    if (param.GetType() != typeof(MovementActionParameters))
                        throw new Exception("Tried to construct Action of type MOVE, but parameters are not MovementActionParameters!");
                    break;
            }

            this.Param = param;
        }

    }

    public abstract class ActionParameters
    { }

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
}
