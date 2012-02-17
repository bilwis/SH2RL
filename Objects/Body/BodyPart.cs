using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL.Objects.Bodies
{
    [Serializable()]
    public class BodyPart:Item
    {
        public String GUID;
        public String name;
        public float surface;
        public float weight;
        public bool IsEssential;

        public int rgb;

        public Body body;
        public BodyPart parent;

        List<Organ> organs;

        public bool IsSevered;
        public bool IsSymetrical; //only for construction/information, organs are already inserted twice!

        /*public BodyPart(String name, float surface, float weight, bool essential)
        {
            GUID = System.Guid.NewGuid().ToString();
            IsSevered = true;
            this.name = name;
            this.surface = surface;
            this.weight = weight;
            this.IsEssential = essential;
            this.organs = new List<Organ>();
        }*/

        public BodyPart(String GUID, String name, float surface, float weight, bool essential, bool symetrical, char display_char, int rgb):
            base(GUID, name, display_char)
        {
            this.GUID = GUID;
            IsSevered = true;
            this.name = name;
            this.surface = surface;
            this.weight = weight;
            this.IsEssential = essential;
            this.IsSymetrical = symetrical;
            this.organs = new List<Organ>();
            this.rgb = rgb;
        }

        public void Sever(int x, int y, int z, MessageHandler msg)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            Init(Util.DecodeRGB(rgb), msg);
            this._desc = "A severed " + name.ToLower() + " lies here.";
        }

        public void ConnectToBody(Body body, BodyPart parent=null)
        {
            cleanup();
            IsSevered = false;
            initialized = false;
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
