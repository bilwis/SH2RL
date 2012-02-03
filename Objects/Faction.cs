using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL.Objects
{
    [Serializable()]
    public enum FactionRelation
    {
        Friendly = 0,
        Hostile = 1,
        Neutral = 2,
        Self = 3
    }

    [Serializable()]
    public class FactionManager
    {
        public Dictionary<string, Faction> factions;

        public FactionManager()
        {
            factions = new Dictionary<string,Faction>();
        }

        public void AddFaction(Faction f)
        {
            factions.Add(f.GUID, f);
        }

        public Faction GetFaction(String guid)
        {
            Faction fac;
            bool exists = factions.TryGetValue(guid, out fac);

            if (!exists)
                return null;

            return factions[guid];
        }
    }

    [Serializable()]
    public class Faction
    {
        String _guid;
        public String GUID
        {
            get
            {
                return _guid.ToString();
            }
        }

        Dictionary<String, FactionRelation> relations;
        bool initialized = false;

        [field:NonSerialized()]
        FactionManager _manager;

        String _name;
        public String Name
        {
            get
            {
                return _name;
            }
        }

        String _desc;
        public String Description
        {
            get
            {
                return _desc;
            }
        }

        public Faction(String name, String desc)
        {
            _guid = System.Guid.NewGuid().ToString();
            _name = name;
            _desc = desc;

            relations = new Dictionary<string, FactionRelation>();
        }

        public Faction(String name, String desc, String guid)
        {
            _guid = guid;
            _name = name;
            _desc = desc;

            relations = new Dictionary<string, FactionRelation>();
        }

        public void Init(FactionManager manager)
        {
            initialized = true;
            _manager = manager;
            _manager.AddFaction(this);
        }

        public void AddRelation(String guid, FactionRelation rel)
        {
            relations.Add(guid, rel);
        }

        public void AddRelation(Faction faction, FactionRelation rel)
        {
            relations.Add(faction.GUID, rel);
        }

        public FactionRelation GetRelation(String guid)
        {
            FactionRelation rel;
            if (relations.TryGetValue(guid, out rel))
                return rel;

            if (guid == _guid)
                return FactionRelation.Self;

            return FactionRelation.Neutral;
        }

        public FactionRelation GetRelation(Faction fac)
        {
            return GetRelation(fac.GUID);
        }

        public void Save()
        {
            initialized = false;
        }
    }
}
