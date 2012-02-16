using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL.Objects.Bodies
{
    public class BodyPart
    {
        public String GUID;
        public String name;
        public float surface;
        public float weight;
        public bool IsEssential;

        public Body body;
        public BodyPart parent;

        List<Organ> organs;

        public bool IsSevered;
        public bool IsSymetrical; //only for construction/information, organs are already inserted twice!

        public BodyPart(String name, float surface, float weight, bool essential)
        {
            GUID = System.Guid.NewGuid().ToString();
            IsSevered = true;
            this.name = name;
            this.surface = surface;
            this.weight = weight;
            this.IsEssential = essential;
            this.organs = new List<Organ>();
        }

        public BodyPart(String GUID, String name, float surface, float weight, bool essential, bool symetrical)
        {
            this.GUID = GUID;
            IsSevered = true;
            this.name = name;
            this.surface = surface;
            this.weight = weight;
            this.IsEssential = essential;
            this.IsSymetrical = symetrical;
            this.organs = new List<Organ>();
        }

        public void ConnectToBody(Body body, BodyPart parent=null)
        {
            cleanup();
            IsSevered = false;
            this.body = body;
            this.parent = parent;
        }

        private void cleanup()
        {
            if (IsSymetrical)
                organs.RemoveAt(organs.Count / 2);
        }

        public void AddOrgan(Organ organ)
        {
            if (!IsSymetrical)
                organs.Add(organ);
            else
            {
                organs.Insert(organs.Count / 2, organ);
                organs.Insert(organs.Count / 2, organ);
            }
        }

    }
}
