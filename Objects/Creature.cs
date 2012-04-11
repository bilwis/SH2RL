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
        public CharStats Stats;
        public WeightedInventory<Item> Inventory;

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
        public double ETakeCostPerWeight = 1;

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

        public void Take(Item i, Map m)
        {
            Actions.Enqueue(new Action(ActionType.Take, new TakeActionParameters(i.GUID, m), this, ETakeCostPerWeight * i.Weight));
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
                if (checkMovement(x, y, z, m))
                {
                    if (isDiag)
                        Actions.Enqueue(new Action(ActionType.Move, new MovementActionParameters(x, y, m.DropObject(x, y, z), m), this, EDiagMovementCost));
                    else
                        Actions.Enqueue(new Action(ActionType.Move, new MovementActionParameters(x, y, m.DropObject(x, y, z), m), this, EMovementCost));
                }
                else
                {
                    Debug.WriteLine("MOVEMENT IMPOSSIBLE");
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
                    MovementActionParameters m_param = (MovementActionParameters)a.Param;
                    if (checkMovement(m_param))
                    {
                        SetPosition(m_param.Target_X, m_param.Target_Y, m_param.Target_Z);
                        return true;
                    }
                    else
                        return false;

                case ActionType.Take:
                    TakeActionParameters t_param = (TakeActionParameters)a.Param;
                    Item item;
                    if (!t_param.map.ItemList.TryGetValue(t_param.guid, out item))
                        return false; //Item not loaded in map

                    if (item.X == x && item.Y == y && item.Z == z)
                    {
                        //Pick up
                        if (!Inventory.Add(item))
                            return false; //Taking item failed (weight)
                        //Remove from map
                        t_param.map.ItemList.Remove(t_param.guid);
                        if (typeof(Player) == this.GetType())
                            _messageHandler.SendMessage("Picked up " + item.Name + "!");

                        return true;
                    }

                    return false; //Item not in same position
            }
            return false;
        }

        protected virtual bool checkMovement(MovementActionParameters param)
        {
            //_messageHandler.SendDebugMessage("CONSIDERING MOVEMENT FOR " + _name + " WITH CREATURE checkMovement()");
            return param.map.IsMovementPossible(param.Target_X, param.Target_Y, param.Target_Z);
        }

        protected virtual bool checkMovement(int x, int y, int z, Map m)
        {
            //_messageHandler.SendDebugMessage("CONSIDERING MOVEMENT FOR " + _name + " WITH CREATURE checkMovement()");
            return m.IsMovementPossible(x,y,z);
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

        public Creature(int x, int y, int z, String name, String desc, char displaychar, Body body, CharStats stats)
        {
            _guid = System.Guid.NewGuid().ToString();
            this.x = x;
            this.y = y;
            this.z = z;
            _name = name;
            _desc = desc;

            _char = displaychar;

            this.Body = body;
            this.Stats = stats;
            Inventory = new WeightedInventory<Item>(Stats.GetCarryCapacity());

            energy = 0;
            energyReg = 1.0d;
        }
    }

    [Serializable()]
    public class Player:Creature
    {
        public LightSource Lightsource;
        public Firearm EquippedWeapon;

        protected override bool checkMovement(MovementActionParameters param)
        {
            //_messageHandler.SendDebugMessage("CONSIDERING MOVEMENT FOR " + _name + " WITH PLAYER checkMovement(param)");
            return param.map.CheckPlayerMovement(param.Target_X, param.Target_Y, param.Target_Z);
        }

        protected override bool checkMovement(int x, int y, int z, Map m)
        {
            //_messageHandler.SendDebugMessage("CONSIDERING MOVEMENT FOR " + _name + " WITH PLAYER checkMovement()");
            return m.CheckPlayerMovement(x, y, z);
        }

        public override void SetPosition(int x, int y, int z)
        {
            Lightsource.SetPosition(x, y, z);
            if (EquippedWeapon != null)
                EquippedWeapon.SetPosition(x, y, z);

            base.SetPosition(x, y, z);
        }

        public void RegisterLightSource(LightSource ls)
        {
            Lightsource = ls;
        }

        public void AttackRanged(Creature target)
        {
            _messageHandler.SendDebugMessage("######################################");
            _messageHandler.SendDebugMessage(Name + " tries to attack " + target.Name + "!");

            BodyPart tar_part = target.Body.GetRandomBodyPart();

            _messageHandler.SendDebugMessage(Name + " targets " + target.Name + "'s " + tar_part.Name + "!");


        }

        public void EquipMagazine(Magazine m)
        {
            if (EquippedWeapon != null)
            {
                EquippedWeapon.Reload(m);
                _messageHandler.SendMessage(Name + " reloads his " + EquippedWeapon.Name + " with a " +  m.Name + ".");
            }
        }

        public void EquipWeapon(Firearm f)
        {
            EquippedWeapon = f;
            EquippedWeapon.SetVisible(false);

            _messageHandler.SendMessage(Name + " picks up a " + EquippedWeapon.Name + ".");
        }

        public override void Init(TCODColor fore, MessageHandler messageHandler, Faction fac, Action firstAction)
        {
            Lightsource.Init(fore, messageHandler);
            Lightsource.Activate();

            if (EquippedWeapon != null)
                EquippedWeapon.Init(fore, messageHandler);

            base.Init(fore, messageHandler, fac, firstAction);
        }

        public Player(int x, int y, int z, String name, String desc, char displaychar, Body body, CharStats stats)
            : base(x, y, z, name, desc, displaychar, body, stats)
        {
            energyReg = 1.1d;
        }
    }
}
