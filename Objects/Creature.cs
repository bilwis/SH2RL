using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

using System.Diagnostics;

using ShootyShootyRL.Mapping;
using ShootyShootyRL.Objects.Bodies;

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
    public class Creature : Object
    {
        [field:NonSerialized()]
        public Queue<Action> Actions;

        public Body Body;

        protected double energy;
        protected double energyReg;

        protected bool flying = false;

        [field: NonSerialized()]
        public Faction Faction; 

        public double Energy
        {
            get
            {
                return energy;
            }
        }

        public double EnergyReg
        {
            get
            {
                return energyReg;
            }
        }

        public double EMovementCost = 15;
        public double EDiagMovementCost = 19;

        public override void SetVisible(bool value)
        {
            _visible = value;
        } 

        public override void SetPosition(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void Move(int x, int y, int z, Map m, bool overrideAll = false)
        {
            bool isDiag = false;

            if (x * y != 0 || x * z != 0 || y * z != 0)
                isDiag = true;

            if (overrideAll)
                Actions.Clear();

            if (!flying)
            {
                if (m.IsMovementPossibleDrop(x, y, z))
                {
                    if (isDiag)
                        Actions.Enqueue(new Action(ActionType.Move, new MovementActionParameters(x, y, m.DropObject(x, y, z), m), this, EDiagMovementCost));
                    else
                        Actions.Enqueue(new Action(ActionType.Move, new MovementActionParameters(x, y, m.DropObject(x, y, z), m), this, EMovementCost));
                }
            }
                //SetPosition(x, y, z);
        }

        public void Tick()
        {
            if (!initialized)
                throw new Exception("Creature not initialized.");

            double cost = 0.0d;

            energy += EnergyReg;
            //_messageHandler.SendMessage("Creature " + _name + " has " + energy + " energy after start of round, with " + Actions.Count + " actions scheduled.");
            while (Actions.Count > 0)
            {
                cost = Actions.Peek().Cost;
                if (energy >= cost)
                {
                    if (doAction(Actions.Dequeue()))
                        energy -= cost;
                }
                //else
                    break;
            }

        }

        protected bool doAction(Action a)
        {
            switch (a.Type)
            {
                case ActionType.Move:
                    MovementActionParameters param = (MovementActionParameters)a.Param;
                    if (checkMovement(param))
                    {
                        SetPosition(param.Target_X, param.Target_Y, param.Target_Z);
                        return true;
                    }
                    else
                        return false;
            }
            return false;
        }

        protected virtual bool checkMovement(MovementActionParameters param)
        {
            //_messageHandler.SendDebugMessage("CONSIDERING MOVEMENT FOR " + _name + " WITH CREATURE checkMovement()");
            return param.map.IsMovementPossible(param.Target_X, param.Target_Y, param.Target_Z);
        }

        //TODO: Actions !
        public virtual void Init(TCODColor color, MessageHandler msg, Faction fac, Action firstAction)
        {
            ForeColor = color;
            _messageHandler = msg;
            this.Faction = fac;

            Actions = new Queue<Action>();
            doAction(firstAction);

            Body.Init(this);

            initialized = true;
            msg.SendDebugMessage("New object created and initialized with GUID " + this.GUID + ", name: " + this.Name + ".");
        }

        public override bool Save()
        {
            initialized = false;
            return true;
        }

        public Creature(int x, int y, int z, String name, String desc, char displaychar, Body body)
        {
            _guid = System.Guid.NewGuid().ToString();
            this.x = x;
            this.y = y;
            this.z = z;
            _name = name;
            _desc = desc;

            _char = displaychar;

            this.Body = body;

            energy = 0;
            energyReg = 1.0d;
        }
    }

    [Serializable()]
    public class Player:Creature
    {
        public LightSource Lightsource;

        protected override bool checkMovement(MovementActionParameters param)
        {
            //_messageHandler.SendDebugMessage("CONSIDERING MOVEMENT FOR " + _name + " WITH PLAYER checkMovement()");
            return param.map.CheckPlayerMovement(param.Target_X, param.Target_Y, param.Target_Z);
        }

        public override void SetPosition(int x, int y, int z)
        {
            Lightsource.SetPosition(x, y, z);
            base.SetPosition(x, y, z);
        }

        public void RegisterLightSource(LightSource ls)
        {
            Lightsource = ls;
        }

        public override void Init(TCODColor fore, MessageHandler messageHandler, Faction fac, Action firstAction)
        {
            Lightsource.Init(fore, messageHandler);
            Lightsource.Activate();

            base.Init(fore, messageHandler, fac, firstAction);
        }

        public Player(int x, int y, int z, String name, String desc, char displaychar, Body body)
            : base(x, y, z, name, desc, displaychar, body)
        {
            energyReg = 1.1d;
        }
    }
}
