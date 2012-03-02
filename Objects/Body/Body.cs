using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ShootyShootyRL.Objects.Bodies
{
    /// <summary>
    /// This class represents a body for a creature consisting of various parts, which are in turn comprised of organs,
    /// which contain multiple organ parts. The body class handles all interfacing with those parts and subparts.
    /// </summary>
    [Serializable()]
    public class Body
    {
        public String GUID;
        [field:NonSerialized()]
        public Creature parent;

        Dictionary<string, BodyPart> parts;
        Dictionary<float, string> hitmap;

        public float height, width, depth;

        bool initialized = false;
        bool loaded = false;

        public Body(string filename)
        {
            this.GUID = System.Guid.NewGuid().ToString();

            parts = new Dictionary<string, BodyPart>();
            ParseBodyDefinition(filename);
        }

        public void Init(Creature parent)
        {
            this.parent = parent;
            initialized = true;
        }

        public void Save()
        {
            initialized = false;
        }

        private void makeHitMap()
        {
            float total_surface = 0.0f;

            foreach (BodyPart bp in parts.Values)
            {
                total_surface += bp.Surface;
            }


        }

        public void AddBodyPart(BodyPart bp, BodyPart parent=null)
        {
            parts.Add(bp.GUID, bp);
            bp.ConnectToBody(this, parent);
        }

        public BodyPart SeverRandomBodyPart()
        {
            Random rand = new Random();
            int i = rand.Next(0, parts.Count);
            BodyPart temp_part =  parts.ElementAt(i).Value.Sever(parent.X, parent.Y, parent.Z, parent.MessageHandler);

            parts.Remove(temp_part.GUID);
            return temp_part;
        }

        public String MakeDescription()
        {
            String temp = "";

            foreach (BodyPart bp in parts.Values)
            {
                temp += bp.Name + ", ";
            }

            temp += ".";

            return temp;
        }

        public void ParseBodyDefinition(string filename)
        {
            Random rand = new Random();
            XmlTextReader reader = new XmlTextReader(filename);
            string name;
            float var = 1.0f;

            List<Organ> organ_defs = new List<Organ>();
            List<OrganConstructor> bp_organs = new List<OrganConstructor>();
            Dictionary<string, string> bp_parents = new Dictionary<string,string>();

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "name":
                                name = reader.ReadElementContentAsString();
                                break;
                            case "height":
                                height = reader.ReadElementContentAsFloat();
                                break;
                            case "width":
                                width = reader.ReadElementContentAsFloat();
                                break;
                            case "depth":
                                depth = reader.ReadElementContentAsFloat();
                                break;
                            case "variation":
                                var = reader.ReadElementContentAsFloat();
                                break;

                            case "organ_def":
                                Organ o = parseOrganDefinition(reader);
                                if (o == null)
                                    throw new Exception("Error while parsing body definition XML: Organ above line " + reader.LineNumber + " not correctly parsed.");
                                organ_defs.Add(o);
                                break;

                            case "body_part":
                                string guid = System.Guid.NewGuid().ToString();

                                string bp_name = "ERROR";
                                float surface = 0.0f;
                                float weight = 0.0f;
                                bool essential = false;

                                int rgb = 0;
                                string display_char = " ";

                                bool symetrical = false;
                                string parent_name = null;

                                bool readingOrgan = false;
                                OrganConstructor temp = new OrganConstructor();

                                bool bp_done = false;

                                while (reader.Read() && !bp_done)
                                {

                                    switch (reader.NodeType)
                                    {
                                        case XmlNodeType.Element:
                                            switch (reader.Name)
                                            {
                                                case "name":
                                                    if (!readingOrgan)
                                                        bp_name = reader.ReadElementContentAsString();
                                                    else
                                                        temp.alias = reader.ReadElementContentAsString();
                                                    break;
                                                case "surface":
                                                    surface = reader.ReadElementContentAsFloat();
                                                    break;
                                                case "weight":
                                                    weight = reader.ReadElementContentAsFloat();
                                                    break;
                                                case "essential":
                                                    essential = reader.ReadElementContentAsBoolean();
                                                    break;
                                                case "parent":
                                                    parent_name = reader.ReadElementContentAsString();
                                                    break;
                                                case "display_char":
                                                    display_char = reader.ReadElementContentAsString();
                                                    break;
                                                case "color":
                                                    string[] temp_col_arr = reader.ReadElementContentAsString().Split(',');
                                                    rgb = Util.EncodeRGB(Int32.Parse(temp_col_arr[0]), Int32.Parse(temp_col_arr[1]), Int32.Parse(temp_col_arr[2]));
                                                    break;
                                                case "symetrical":
                                                    symetrical = reader.ReadElementContentAsBoolean();
                                                    break;
                                                case "organ":
                                                    if (readingOrgan)
                                                        throw new Exception("Error while parsing body definition XML: Organ element not closed before line " + reader.LineNumber + ".");
                                                    readingOrgan = true;
                                                    temp = new OrganConstructor();
                                                    //set default
                                                    temp.hit_prob = 1.0f;
                                                    break;
                                                case "type":
                                                    temp.name = reader.ReadElementContentAsString();
                                                    break;
                                                case "thickness":
                                                    temp.thickness = reader.ReadElementContentAsFloat();
                                                    break;
                                                case "hit_prob":
                                                    temp.hit_prob = reader.ReadElementContentAsFloat();
                                                    break;
                                            }
                                            break;
                                        case XmlNodeType.EndElement:
                                            switch (reader.Name)
                                            {
                                                case "organ":
                                                    readingOrgan = false;
                                                    temp.bp_guid = guid;
                                                    bp_organs.Add(temp);
                                                    break;
                                                case "body_part":
                                                    BodyPart new_bp = new BodyPart(guid, bp_name, surface, weight, essential, symetrical, display_char[0], rgb);
                                                    bp_parents.Add(new_bp.GUID, parent_name);
                                                    parts.Add(new_bp.GUID, new_bp);
                                                    bp_done = true;
                                                    break;
                                            }
                                            break;
                                    }
                                }

                                break;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        switch (reader.Name)
                        {
                            

                        }
                        break;
                }
            }

            //load bp_organs into bodyparts, attach bodyparts to Body
            foreach (BodyPart p in parts.Values)
            {
                foreach (OrganConstructor oc in bp_organs)
                {
                    if (p.GUID == oc.bp_guid)
                    {
                        foreach (Organ o in organ_defs)
                        {
                            if (o.name == oc.name)
                            {
                                p.AddOrgan(new Organ(o, oc.thickness, oc.hit_prob, oc.alias));
                            }
                        }
                    }
                }
                if (bp_parents[p.GUID] != null)
                {
                    foreach (BodyPart par in parts.Values)
                    {
                        if (par.Name == bp_parents[p.GUID])
                            p.ConnectToBody(this, par);
                    }
                }
                else
                {
                    p.ConnectToBody(this);
                }
            }


            this.height = (float)(height + (var * (rand.NextDouble() - 0.5)));
            this.width = (float)(width + (var * (rand.NextDouble() - 0.5)));
            this.depth = (float)(depth + (var * (rand.NextDouble() - 0.5)));
            loaded = true;
        }

        private Organ parseOrganDefinition(XmlTextReader reader)
        {
            string name = "ERROR";
            bool exclusive = false;

            List<OrganPart> parts = new List<OrganPart>();
            OrganPart temp = new OrganPart();
            bool readingPart=false;

            temp.critical = false;  //default
            
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "part":
                                if (readingPart)
                                    throw new Exception("Error while parsing body definition XML: New part element before end part element");
                                readingPart = true;
                                break;
                            case "name":
                                if (!readingPart)
                                    name = reader.ReadElementContentAsString();
                                else
                                    temp.name = reader.ReadElementContentAsString();
                                break;
                            case "exclusive":
                                exclusive = reader.ReadElementContentAsBoolean();
                                break;
                            case "hit_prob":
                                temp.hit_prob = reader.ReadElementContentAsFloat();
                                break;
                            case "pain":
                                temp.pain = reader.ReadElementContentAsFloat();
                                break;
                            case "blood_loss":
                                temp.blood_loss = reader.ReadElementContentAsFloat();
                                break;
                            case "resistance":
                                temp.resistance = reader.ReadElementContentAsFloat();
                                break;
                            case "on_hit_kill":
                                temp.critical = reader.ReadElementContentAsBoolean();
                                break;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        switch (reader.Name)
                        {
                            case "part":
                                readingPart = false;
                                parts.Add(temp);
                                temp = new OrganPart();
                                break;
                            case "organ_def":
                                Organ self = new Organ(name, exclusive);
                                foreach (OrganPart op in parts)
                                {
                                    self.AddPart(op);
                                }
                                return self;
                        }
                        break;
                }
            }

            return null;
        }
        /*
        private BodyPart parseBodyPartDefinition(XmlTextReader reader)
        {
            string name = "ERROR";
            float Surface = 0.0f;
            float Weight = 0.0f;
            bool IsEssential = false;

            string parent_name = null;
            bool symetrical = false;

            bool readingOrgan = false;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "name":
                                name = reader.ReadElementContentAsString();
                                break;
                            case "Surface":
                                Surface = reader.ReadElementContentAsFloat();
                                break;
                            case "Weight":
                                Weight = reader.ReadElementContentAsFloat();
                                break;
                            case "IsEssential":
                                IsEssential = reader.ReadElementContentAsBoolean();
                                break;
                            case "parent":
                                parent_name = reader.ReadElementContentAsString();
                                break;
                            case "organ":
                                if (readingOrgan)
                                    throw new Exception("Error while parsing Body definition XML: Organ element not closed before line " + reader.LineNumber + ".");
                                readingOrgan = true;
                                symetrical = false;

                                break;
                            case "symetrical":
                                symetrical = reader.ReadElementContentAsBoolean();
                                break;

                        }
                        break;
                    case XmlNodeType.EndElement:
                        switch (reader.Name)
                        {
                            case "":
                                break;
                            
                        }
                        break;
                }
            }
        }
         * */
    }

    public struct OrganConstructor
    {
        public string bp_guid;
        public string name;
        public float thickness;
        public float hit_prob;
        public string alias;
    }
}
