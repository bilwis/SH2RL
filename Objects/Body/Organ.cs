using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL.Objects.Bodies
{
    public class Organ
    {
        public String name;
        public String alias = null;
        public bool exclusive;
        public float thickness = 0.0f;
        public float hit_prob = 1.0f;

        List<OrganPart> parts;

        public Organ(String name, bool exclusive)
        {
            this.name = name;
            this.exclusive = exclusive;
            parts = new List<OrganPart>();
        }

        public Organ(Organ o, float thickness, float hit_prob, string alias=null)
        {
            this.name = o.name;
            this.exclusive = o.exclusive;
            this.parts = new List<OrganPart>(o.parts);
            this.thickness = thickness;
            this.hit_prob = hit_prob;
            this.alias = alias;
        }

        public void AddPart(String name, float hit_prob, float pain, float blood_loss, float resistance, bool critical=false)
        {
            parts.Add(new OrganPart(name, hit_prob, pain, blood_loss, resistance, critical));
        }

        public void AddPart(OrganPart part)
        {
            parts.Add(part);
        }

        public String Hit()
        {
            throw new NotImplementedException();
        }

        //TODO: Idea: Butchering converting Organ(Part) -> Item? 
    }

    public struct OrganPart
    {
        public String name;
        public float hit_prob;
        public float pain;
        public float blood_loss;
        public float resistance;
        public bool critical;

        public OrganPart(String name, float hit_prob, float pain, float blood_loss, float resistance, bool critical)
        {
            this.name = name;
            this.hit_prob = hit_prob;
            this.pain = pain;
            this.blood_loss = blood_loss;
            this.resistance = resistance;
            this.critical = critical;
        }
    }
}
