using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;
using System.Diagnostics;
using System.Data.SQLite;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using ShootyShootyRL.Objects;

namespace ShootyShootyRL.Mapping
{
    public class Map
    {
        private static bool DEBUG_OUTPUT = true;

        Cell[, ,] cells;
        WorldMap wm;
        public Creature Player;
        public Dictionary<String, Creature> CreatureList;
        public Dictionary<String, double> CreaturesByDistance;
        public Dictionary<String, Item> ItemList;

        public static int VIEWPORT_WIDTH = 10;
        public static int VIEWPORT_HEIGHT = 10;
        public static int VIEW_DISTANCE_TILES_Z = 10;
        public static int VIEW_DISTANCE_CREATURES_DOWN_Z = 3;
        public static int VIEW_DISTANCE_CREATURES_UP_Z = 1;

        private int currentCellId;

        private MessageHandler _out;
        private FactionManager facman; 
        private SQLiteConnection dbconn;

        private byte[] test;

        public Map(Creature player, WorldMap wm, MessageHandler _out, FactionManager facman, SQLiteConnection dbconn)
        {
            this.Player = player;
            this.wm = wm;
            this._out = _out;
            this.dbconn = dbconn;


            CreatureList = new Dictionary<string, Creature>();
            CreaturesByDistance = new Dictionary<string, double>();
            ItemList = new Dictionary<string, Item>();
            this.facman = facman;

            cells = new Cell[3, 3, 3];
            centerAndLoad();
            Console.WriteLine(getTileFromCells(Player.X, Player.Y, Player.Z).Description);

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(i + ": " + getTileFromCells(Player.X, Player.Y, i).Name);
            }

        }

        private void centerAndLoad()
        {
            cells[1, 1, 1] = wm.GetCellFromCoordinates(Player.X, Player.Y, Player.Z);
            cells[1, 1, 1].Load();
            currentCellId = cells[1, 1, 1].CellID;

            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    for (int z = -1; z < 2; z++)
                    {
                        if (!(x == 0 && y == 0 && z == 0))
                        {
                            cells[1 + x, 1 + y, 1 + z] = wm.GetAdjacentCell(x, y, z, cells[1, 1, 1]);
                            cells[1 + x, 1 + y, 1 + z].Load();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This function handles the map side of player movement. It will check on the viability
        /// of the move and, if necessary, load new Cells and dispose the old ones.
        /// </summary>
        /// <param name="abs_x"></param>
        /// <param name="abs_y"></param>
        /// <param name="abs_z"></param>
        /// <returns>true if the movement is possible, false otherwise</returns>
        public bool MovePlayer(int abs_x, int abs_y, int abs_z)
        {
            int cell_x = -1;
            int cell_y = -1;
            int cell_z = -1;
            int[] cell_coords = new int[3];
            int relCellID;

            relCellID = wm.GetCellIDFromCoordinates(abs_x, abs_y, abs_z);

            //Can't fly!
            if (!IsMovementPossibleDrop(abs_x, abs_y, abs_z))
            {
                Debug.WriteLine("NAY!");
                return false;
            }

            //Not the same cell anymore? Better load the new ones!
            if (relCellID != currentCellId)
            {
                //Get coordinates of now player-holding cell relative to previous
                cell_coords = getInternalCellPosFromID(relCellID);
                if (cell_coords == null)
                    throw new Exception("Error while trying to move player: New cell was not found in Map Cell array!");

                cell_x = cell_coords[0];
                cell_y = cell_coords[1];
                cell_z = cell_coords[2];

                if (loadNewCells(cell_x, cell_y, cell_z) == false)
                    return false;
                
                if (Map.DEBUG_OUTPUT)
                {
                    _out.SendMessage("Entered cell #" + relCellID + " relative to old cell: x:" + cell_x + " y: " + cell_y + ".");
                }
                currentCellId = relCellID;
            }
            return true;
        }

        #region "Loading and Saving"

        private bool loadNewCells(int rel_x, int rel_y, int rel_z)
        {
            //NOTE: The following code seems really complicated when you look at it (it's very "clean" and "efficient", ergo totally not human readable!)
            //I will try to explain the meaning of each loop/vector by using the example "MOVE INTO CELL TO THE TOP (x-1) LEFT (y-1) OF THE CURRENT CENTER".
            //This would correspond to the parameters: rel_x = 0, rel_y = 0, rel_z = 1. 
            //When moving into the "top left", the program must do the following operations: 
            //  0) CHECK if the move is legal.
            //     The program checks if all the cells that would have to be loaded (see III) ) actually exist, if not, it denies the move by
            //     returning false.
            //  I) UNLOAD the "old" cells, i.e. the cells that now fall out of the 3x3 "loaded cells" grid, to free up the memory they're holding.
            //     For our example, that means all the "lowermost" and the "rightmost" cells must be unloaded.
            // II) SHIFT the remaining original cells.
            //     Since we moved to the top left, the former top left (TL) cell is now the central cell. The top top (TT), the left left (LL)
            //     and the former center (C) can also be preserved, but must be shifted too. 
            //     In the example the "shifting vector" (in the code: shift_vect) is {-1,-1,0} and the 
            //     "limiting vector" (in the code: limit_vect) is {0,0,-1}, meaning that all cells where X != 0 AND Y != 0
            //     (Z doesn't matter, it's "taken along", hence the -1 in the limit_vect) are overwritten with the cell at {X+(-1), Y+(-1), Z+0}
            //     This shifts the TL, the TT, the LL and the C to the C, the RR, the BB and the BR, respectively.
            //III) LOAD the new cells.
            //     All that remains is that we load the new cells, in our example case these would be the new TL, TT, TR, LL and BL, because we
            //     moved both on the x- and on the y-axis. Again, this is handled by the shifting and the limiting vector. The limiting vector
            //     now (figuratively) becomes the "limited"-vector (still {0,0,-1}) because only cells where X == 0 OR Y == 0 are newly loaded.
            //     The loading uses the GetAdjacentCell method of the WorldMap-Class and utilized the shifting vector.

            //FOR DEBUG PURPOSES
            Stopwatch sw = new Stopwatch();
            sw.Start();

            //1. Setup Vectors
            int[] vect = { rel_x, rel_y, rel_z };
            int[] unload_vect = { -1, -1, -1 };
            int[] shift_vect = { 0, 0, 0 };

            //1.1 Initialize the "unload vector"
            for (int i = 0; i < 3; i++)
            {
                if (vect[i] == 0)
                    unload_vect[i] = 2; //If shift on the "i"-Axis is -1, or "0" in the relative coords, 
                                        //then the cells with "i+1" must be unloaded.

                if (vect[i] == 1)
                    unload_vect[i] = -1;//If shift on the "i"-Axis is 0, or "1" in the relative coords, 
                                        //then this axis doesn't warrant any action.

                if (vect[i] == 2)
                    unload_vect[i] = 0; //If shift on the "i"-Axis is +1, or "2" in the relative coords, 
                                        //then the cells with "i-1" must be unloaded.

            }

            //1.2 Convert the "relative" coordinates of the new cell (stored in vect[]) into the necessary
            //    shifting operation on each axis.
            for (int i = 0; i < 3; i++)
            {
                if (vect[i] == 0)
                    shift_vect[i] = -1;

                if (vect[i] == 1)
                    shift_vect[i] = 0;

                if (vect[i] == 2)
                    shift_vect[i] = 1;
            }

            //1.3 This vector stores the information necessary to prevent the algorithm from trying
            //    to shift non-loaded cells into the cell array. It is also used (conversely) to load
            //    only the cells that need loading.
            int[] limit_vect = { -1, -1, -1 };
            for (int i = 0; i < 3; i++)
            {
                if (vect[i] == 0)
                    limit_vect[i] = 0;

                if (vect[i] == 1)
                    limit_vect[i] = -1;

                if (vect[i] == 2)
                    limit_vect[i] = 2;
            }

            //---------------------------------------------------------------------------------
            //0) RUN PRE-ALGORITHM CHECK if cells to be loaded actually exists,
            //   if not, deny the move. 
            //
            //   NOTE: This has to be run previous to every other operation, because the
            //   initial state is not backed up for memory reasons, so "undo" later is impossible
            //---------------------------------------------------------------------------------
            bool[] causes_validation = { false, false, false };
            for (int x = 0; x < 3; x++)
            {
                if (x == limit_vect[0] || limit_vect[0] == -1)
                {
                    causes_validation[0] = true;
                }
                for (int y = 0; y < 3; y++)
                {
                    if (y == limit_vect[1] || limit_vect[1] == -1)
                    {
                        causes_validation[1] = true;
                    }
                    for (int z = 0; z < 3; z++)
                    {
                        if (z == limit_vect[2] || limit_vect[2] == -1)
                        {
                            causes_validation[2] = true;
                        }

                        if (!causes_validation[0] && !causes_validation[1] && !causes_validation[2])
                            continue;

                        if (wm.GetAdjacentCell(shift_vect[0], shift_vect[1], shift_vect[2], cells[x, y, z]) == null)
                            return false;

                        causes_validation[2] = false;
                    }
                    causes_validation[1] = false;
                }
                causes_validation[0] = false;
            }

            //---------------------------------------------------------------------------------
            //I) UNLOAD OLD CELLS, i.e. the cells on the "far end" of the coordinate block
            //---------------------------------------------------------------------------------
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        if (x == unload_vect[0] || y == unload_vect[1] || z == unload_vect[2])
                        {
                            unloadCellContent(cells[x, y, z]);
                            cells[x, y, z].Unload();
                        }
                    }
                }
            }

            //---------------------------------------------------------------------------------
            //II) SHIFT remaining original cells in the appropriate direction
            //---------------------------------------------------------------------------------

            //Setup temporary cells array (this is needed to ensure neither loading or shifting
            // is done on cells that have already been shifted or loaded)
            Cell[,,] tempcells = new Cell[3,3,3];
            Array.Copy(cells, tempcells, cells.Length);

            //Do the shifting
            for (int x = 0; x < 3; x++)
            {
                if (x != limit_vect[0])
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (y != limit_vect[1])
                        {
                            for (int z = 0; z < 3; z++)
                            {
                                if (z != limit_vect[2])
                                {
                                    cells[x, y, z] = tempcells[x + shift_vect[0], y + shift_vect[1], z + shift_vect[2]];
                                }
                            }
                        }
                    }
                }
            }

            //---------------------------------------------------------------------------------
            //III) LOAD new cells
            //---------------------------------------------------------------------------------
            //Do the loading
            int derp = 0;
            bool[] cause_load = { false, false, false };
            for (int x = 0; x < 3; x++)
            {
                if (x == limit_vect[0])
                {
                    cause_load[0] = true;
                }
                    for (int y = 0; y < 3; y++)
                    {
                        if (y == limit_vect[1])
                        {
                            cause_load[1] = true;
                        }
                            for (int z = 0; z < 3; z++)
                            {
                                if (z == limit_vect[2])
                                {
                                    cause_load[2] = true;
                                }

                                if (!cause_load[0] && !cause_load[1] && !cause_load[2])
                                    continue;

                                cells[x, y, z] = wm.GetAdjacentCell(shift_vect[0], shift_vect[1], shift_vect[2], tempcells[x, y, z]);
                                cells[x, y, z].Load();
                                loadCellContent(cells[x, y, z].CellID);
                                derp++;
                                cause_load[2] = false;
                            }
                            cause_load[1] = false;
                    }
                    cause_load[0] = false;
            }

            tempcells = null;

            sw.Stop();
            _out.SendMessage(derp + " new cells loaded: Loading took " + sw.ElapsedMilliseconds + "ms.");

            return true;
        }

        /// <summary>
        /// This function deserializes an item from the item database.
        /// </summary>
        /// <param name="guid">The guid of the item to deserialize.</param>
        public bool LoadItem(string guid)
        {
            //The item's data is retrieved from the DB using the GUID.
            //The "data" column is an ASCII-Encoded byte array which is then
            //converted into a string which is then converted into an "hex-encoded"
            //or proper byte-array. The resulting byte-array is then written into
            //a MemoryStream from which the Item is deserialized.

            byte[] data;
            int color;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter deserializer = new BinaryFormatter();

            SQLiteDataReader reader;
            if (!checkIsInDatabase(Util.DBLookupType.Item, guid, out reader))
                return false;
            
            String str = Util.ByteArrayToString((byte[])reader[1]);
            data = new byte[str.Length / 2];

            for (int n = 0; n < str.Length/2; n++)
            {
                data[n] = byte.Parse(str.Substring(n*2, 2), System.Globalization.NumberStyles.HexNumber);
            }

            color = Convert.ToInt32(Util.ByteArrayToString((byte[])reader[2]));
            
            fstream.Write(data, 0, data.Length);
            fstream.Seek(0, SeekOrigin.Begin);

            Item i = (Item)deserializer.Deserialize(fstream);
            i.Init(new TCODColor(color >> 16, color >> 8 & 0xFF, color & 0xFF), _out);

            AddItem(i);

            reader.Close();
            reader.Dispose();
            fstream.Close();

            return true;
        }

        /// <summary>
        /// This function serializes an item to the item database.
        /// </summary>
        /// <param name="guid">The guid of the item to serialize.</param>
        public void UnloadItem(string guid)
        {
            //The item is retrieved using the given guid, then serialized to the 
            //MemoryStream which is then written into the database. For the DB
            //the MemoryStream (which holds a byte array) is converted into a string
            //(after the pattern: [1A, 20, 00, FF] -> 1A2000FF)

            Item i;

            byte[] arr;
            string data;
            int color;
            bool update = false;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter serializer = new BinaryFormatter();
            SQLiteCommand command = new SQLiteCommand(dbconn);

            //Check if item already is in DB
            if (checkIsInDatabase(Util.DBLookupType.Item, guid))
                update = true;

            SQLiteTransaction tr = dbconn.BeginTransaction();
            //command = new SQLiteCommand("", dbconn, tr);
            command.Transaction = tr;

            i = ItemList[guid];

            i.Save();

            serializer.Serialize(fstream, i);

            arr = new byte[fstream.Length];
            color = i.ForeColor.Red << 16 | i.ForeColor.Green << 8 | i.ForeColor.Blue;
            fstream.Seek(0, SeekOrigin.Begin);
            fstream.Read(arr, 0, (int)fstream.Length);

            data = BitConverter.ToString(arr).Replace("-", string.Empty);

            if (!update)
                command.CommandText = "INSERT INTO items (guid, data, color) VALUES ('" + guid + "', '" + data + "', " + color + ")";
            if (update)
                command.CommandText = "UPDATE items SET data='" + data + "', color=" + color + " WHERE guid='" + guid + "'";

            command.ExecuteNonQuery();
            tr.Commit();
                
            ItemList.Remove(guid);

            command.Dispose();
            tr.Dispose();
            fstream.Close();
        }

        public bool LoadAICreature(string guid)
        {
            AICreature c;
            Faction _fac;
            AI _ai;

            byte[] data;
            string fac_id, ai_id;
            int color;

            bool fac_deser = false;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter deserializer = new BinaryFormatter();
            SQLiteDataReader reader;

            //****DESERIALIZE AI CREATURE****//
            if (!checkIsInDatabase(Util.DBLookupType.AICreature, guid, out reader))
                throw new Exception("Error while trying to load AICreature: No AICreature entry with guid " + guid + " found.");

            //Parse ASCII-Encoded byte array to Hex-Encoded byte array
            String str = Util.ByteArrayToString((byte[])reader[1]);
            data = new byte[str.Length / 2];

            for (int n = 0; n < str.Length / 2; n++)
            {
                data[n] = byte.Parse(str.Substring(n * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }

            fac_id = Util.ByteArrayToString((byte[])reader[2]);
            ai_id = Util.ByteArrayToString((byte[])reader[3]);
            color = Convert.ToInt32(Util.ByteArrayToString((byte[])reader[4]));

            fstream.Write(data, 0, data.Length);
            fstream.Seek(0, SeekOrigin.Begin);

            c = (AICreature)deserializer.Deserialize(fstream);

            //****DESERIALIZE OR LOAD FACTION****//
            _fac = facman.GetFaction(fac_id);
            if (_fac == null)
            {
                reader.Dispose();
                if (!checkIsInDatabase(Util.DBLookupType.Faction, fac_id, out reader))
                    throw new Exception("Error while trying to load AICreature: No Faction entry with guid " + fac_id + " found."); ;

                //Parse ASCII-Encoded byte array to Hex-Encoded byte array
                str = Util.ByteArrayToString((byte[])reader[1]);
                data = new byte[str.Length / 2];

                for (int n = 0; n < str.Length / 2; n++)
                {
                    data[n] = byte.Parse(str.Substring(n * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }

                fstream.SetLength(0);
                fstream.Seek(0, SeekOrigin.Begin);

                fstream.Write(data, 0, data.Length);
                fstream.Seek(0, SeekOrigin.Begin);

                _fac = (Faction)deserializer.Deserialize(fstream);
                fac_deser = true;
            }

            //****DESERIALIZE AI****//
            if (!checkIsInDatabase(Util.DBLookupType.AI, ai_id, out reader))
                throw new Exception("Error while trying to load AICreature: No AI entry with guid " + ai_id + " found."); ;

            //Parse ASCII-Encoded byte array to Hex-Encoded byte array
            str = Util.ByteArrayToString((byte[])reader[1]);
            data = new byte[str.Length / 2];

            for (int n = 0; n < str.Length / 2; n++)
            {
                data[n] = byte.Parse(str.Substring(n * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }

            fstream.SetLength(0);
            fstream.Seek(0, SeekOrigin.Begin);

            fstream.Write(data, 0, data.Length);
            fstream.Seek(0, SeekOrigin.Begin);

            _ai = (AI)deserializer.Deserialize(fstream);

            //****INITIALIZE OBJECTS****//
            if (fac_deser)
                _fac.Init(facman);

            c.Init(new TCODColor(color >> 16, color >> 8 & 0xFF, color & 0xFF), _out, _fac, new Objects.Action(ActionType.Idle, null, c, 0.0d), _ai, this);

            AddCreature(c);
            
            //****CLEANUP****//
            reader.Dispose();
            reader.Close();

            fstream.Close();

            return true;
        }

        /// <summary>
        /// This function serializes a creature to the creature database.
        /// </summary>
        /// <param name="guid">The guid of the creature to serialize.</param>
        public bool UnloadAICreature(string guid)
        {
            AICreature c;

            byte[] arr;
            string data;
            int color;

            bool update = false;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter serializer = new BinaryFormatter();
            SQLiteCommand command = new SQLiteCommand(dbconn);

            //****SERIALIZE AICREATURE****//

            //Check if creature actually is an AICreature
            if (CreatureList[guid].GetType() != typeof(AICreature))
                return false;

            //Check if creature already is in DB
            if (checkIsInDatabase(Util.DBLookupType.AICreature, guid))
                update = true;

            //Initialize Transaction for quick handling of several queries.
            SQLiteTransaction tr = dbconn.BeginTransaction();
            command.Transaction = tr;

            //Serialize the creature
            c = (AICreature)CreatureList[guid];

            c.Save();

            serializer.Serialize(fstream, c);

            arr = new byte[fstream.Length];

            fstream.Seek(0, SeekOrigin.Begin);
            fstream.Read(arr, 0, (int)fstream.Length);

            data = BitConverter.ToString(arr).Replace("-", string.Empty);

            color = c.ForeColor.Red << 16 | c.ForeColor.Green << 8 | c.ForeColor.Blue;

            //Prepare and execute query to insert/update creature
            if (!update)
                command.CommandText = "INSERT INTO ai_creatures (guid, data, faction_id, ai_id, color) VALUES ('" + guid + "', '" + data + "', '" +  c.Faction.GUID + "', '" + c.AI.GUID + "', " + color + ")";
            if (update)
                command.CommandText = "UPDATE ai_creatures SET data='" + data + "', faction_id='" +  c.Faction.GUID + "', ai_id='" + c.AI.GUID + "', color=" + color + " WHERE guid='" + guid + "'";
            
            command.ExecuteNonQuery();

            //****SERIALIZE AI****//
            update = false;
            if (checkIsInDatabase(Util.DBLookupType.AI, c.AI.GUID))
                update = true;

            AI _ai = c.AI;
            _ai.Save();

            fstream.SetLength(0);
            fstream.Seek(0, SeekOrigin.Begin);

            serializer.Serialize(fstream, _ai);

            arr = new byte[fstream.Length];

            fstream.Seek(0, SeekOrigin.Begin);
            fstream.Read(arr, 0, (int)fstream.Length);

            data = BitConverter.ToString(arr).Replace("-", string.Empty);

            
            if (!update)
                command.CommandText = "INSERT INTO ai (guid, data) VALUES ('" + _ai.GUID + "', '" + data + "')";
            if (update)
                command.CommandText = "UPDATE ai SET data='" + data + "' WHERE guid='" + _ai.GUID + "'";

            command.ExecuteNonQuery();

            //****SERIALIZE FACTION****//
            update = false;
            if (checkIsInDatabase(Util.DBLookupType.Faction, c.Faction.GUID))
                update = true;

            Faction _fac = c.Faction;
            _fac.Save();

            fstream.SetLength(0);
            fstream.Seek(0, SeekOrigin.Begin);

            serializer.Serialize(fstream, _fac);

            arr = new byte[fstream.Length];

            fstream.Seek(0, SeekOrigin.Begin);
            fstream.Read(arr, 0, (int)fstream.Length);

            data = BitConverter.ToString(arr).Replace("-", string.Empty);

            if (!update)
                command.CommandText = "INSERT INTO factions (guid, data) VALUES ('" + _fac.GUID + "', '" + data + "')";
            if (update)
                command.CommandText = "UPDATE factions SET data='" + data + "' WHERE guid='" + _fac.GUID + "'";

            command.ExecuteNonQuery();

            //Commit Transaction
            tr.Commit();

            //****CLEANUP****//
            CreatureList.Remove(guid);
            CreaturesByDistance.Remove(guid);

            command.Dispose();
            fstream.Close();
            return true;
        }

        private bool checkIsInDatabase(Util.DBLookupType lookup, string guid, out SQLiteDataReader reader)
        {
            SQLiteCommand command = new SQLiteCommand(dbconn);
            switch (lookup)
            {
                case (Util.DBLookupType.AICreature):
                    command.CommandText = "SELECT * FROM ai_creatures WHERE guid='" + guid + "'";
                    break;
                case (Util.DBLookupType.Item):
                    command.CommandText = "SELECT * FROM items WHERE guid='" + guid + "'";
                    break;
                case (Util.DBLookupType.AI):
                    command.CommandText = "SELECT * FROM ai WHERE guid='" + guid + "'";
                    break;
                case (Util.DBLookupType.Faction):
                    command.CommandText = "SELECT * FROM factions WHERE guid='" + guid + "'";
                    break;
                default:
                    reader = null;
                    return false;
            }

            reader = command.ExecuteReader();
            reader.Read();

            if (reader[0] != DBNull.Value)
                return true;

            return false;
        }

        private bool checkIsInDatabase(Util.DBLookupType lookup, string guid)
        {
            SQLiteDataReader derp;
            return checkIsInDatabase(lookup, guid, out derp);
        }

        private bool unloadCellContent(Cell c)
        {
            SQLiteCommand command = new SQLiteCommand(dbconn);

            //Clear all cell content from DB
            command.CommandText = "DELETE FROM cell_contents WHERE cell_id='" + c.CellID + "'";
            command.ExecuteNonQuery();

            //Setup Transaction
            SQLiteTransaction tr = dbconn.BeginTransaction();
            command.Transaction = tr;

            //Save all items
            string[] keys = ItemList.Keys.ToArray<string>();
            for (int i = 0; i < keys.Length; i++)
            {
                Item kv = ItemList[keys[i]];
                if (kv.X <= (c.X + WorldMap.CELL_WIDTH) && kv.X >= c.X)
                {
                    if (kv.Y <= (c.Y + WorldMap.CELL_HEIGHT) && kv.Y >= c.Y)
                    {
                        if (kv.Z <= (c.Z + WorldMap.CELL_DEPTH) && kv.Z >= c.Z)
                        {
                            command.CommandText = "INSERT INTO cell_contents (cell_id, content_type, content_guid) VALUES ('" + c.CellID + "', '" + (int)Util.DBLookupType.Item + "', '" + kv.GUID + "')";
                            command.ExecuteNonQuery();
                            UnloadItem(kv.GUID);
                        }
                    }
                }
            }

            //Save all creatures
            keys = new string[CreatureList.Count];
            keys = CreatureList.Keys.ToArray<string>();

            for (int i = 0; i < keys.Length; i++)
            {
                Creature cr = CreatureList[keys[i]];
                if (cr.GetType() == typeof(AICreature))
                {
                    if (cr.X <= (c.X + WorldMap.CELL_WIDTH) && cr.X >= c.X)
                    {
                        if (cr.Y <= (c.Y + WorldMap.CELL_HEIGHT) && cr.Y >= c.Y)
                        {
                            if (cr.Z <= (c.Z + WorldMap.CELL_DEPTH) && cr.Z >= c.Z)
                            {
                                command.CommandText = "INSERT INTO cell_contents (cell_id, content_type, content_guid) VALUES ('" + c.CellID + "', '" + (int)Util.DBLookupType.AICreature + "', '" + cr.GUID + "')";
                                command.ExecuteNonQuery();
                                UnloadAICreature(cr.GUID);
                            }
                        }
                    }
                }
            }

            tr.Commit();
            command.Dispose();

            return false;
        }

        private void loadCellContent(int id)
        {
            SQLiteCommand command = new SQLiteCommand(dbconn);

            //Search for the cells content references
            command.CommandText = "SELECT * FROM cell_contents WHERE cell_id='" + id + "'";
            SQLiteDataReader reader = command.ExecuteReader();

            //Load them items
            int item_type;
            string content_guid;

            while (reader.Read())
            {
                content_guid = Util.ByteArrayToString((byte[])reader[2]);
                item_type = Int32.Parse(Util.ByteArrayToString((byte[])reader[1]));
                if (item_type == (int)Util.DBLookupType.Item)
                    LoadItem(content_guid);
                if (item_type == (int)Util.DBLookupType.AICreature)
                    LoadAICreature(content_guid);
            }

            reader.Close();
            reader.Dispose();
            command.Dispose();
        }

        #endregion


        /// <summary>
        /// This function returns an array holding the Map-class-internal coordinates of a cell
        /// with a given ID, or null if the cell is not in the maps cell array.
        /// </summary>
        /// <param name="cellid"></param>
        /// <returns>An array in which the first value is the X, second is Y and third is Z, or null.</returns>
        private int[] getInternalCellPosFromID(int cellid)
        {
            int[] coord = new int[3];

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        if (cells[x, y, z].CellID == cellid)
                        {
                            coord[0] = x;
                            coord[1] = y;
                            coord[2] = z;
                            return coord;
                        }
                    }
                }
            }

            return null;
        }

        private byte getTileIDFromCells(int abs_x, int abs_y, int abs_z)
        {
            int x = 0, y = 0, z = 0;
            if (abs_x < cells[0, 0, 0].X || abs_x > cells[2, 0, 0].X + WorldMap.CELL_WIDTH)
                return 0;
            if (abs_y < cells[0, 0, 0].Y || abs_y > cells[0, 2, 0].Y + WorldMap.CELL_HEIGHT)
                return 0;
            if (abs_z < cells[0, 0, 0].Z || abs_z >= cells[0, 0, 2].Z + WorldMap.CELL_DEPTH)
                return 0;

            x = (int)((abs_x - cells[0, 0, 0].X) / WorldMap.CELL_WIDTH);
            y = (int)((abs_y - cells[0, 0, 0].Y) / WorldMap.CELL_HEIGHT);
            z = (int)((abs_z - cells[0, 0, 0].Z) / WorldMap.CELL_DEPTH);

            return cells[x, y, z].GetTileID(abs_x, abs_y, abs_z);

        }
        
        private Tile getTileFromCells(int abs_x, int abs_y, int abs_z)
        {
            int x = 0, y = 0, z = 0;
            if (abs_x < cells[0, 0, 0].X || abs_x > cells[2, 0, 0].X + WorldMap.CELL_WIDTH)
                return null;
            if (abs_y < cells[0, 0, 0].Y || abs_y > cells[0, 2, 0].Y + WorldMap.CELL_HEIGHT)
                return null;
            if (abs_z < cells[0, 0, 0].Z || abs_z > cells[0, 0, 2].Z + WorldMap.CELL_DEPTH)
                return null;

            x = (int)((abs_x - cells[0, 0, 0].X) / WorldMap.CELL_WIDTH);
            y = (int)((abs_y - cells[0, 0, 0].Y) / WorldMap.CELL_HEIGHT);
            z = (int)((abs_z - cells[0, 0, 0].Z) / WorldMap.CELL_DEPTH);

            return cells[x, y, z].GetTile(abs_x, abs_y, abs_z);
                     
        }
        
        public int GetGround(int abs_x, int abs_y)
        {
            throw new NotImplementedException();

            string t = "Air", t2;

            for (int i = cells[1, 1, 2].Z + WorldMap.CELL_DEPTH - 1; i > cells[1, 1, 0].Z; i--)
            {
                t2 = t;
                t = getTileFromCells(abs_x, abs_y, i).Name;
                if (t != "Air" && t2 == "Air")
                    return i +1;
            }

            return -1;

            /*
            string t = "Air", t2;

            for (int i = cells[1,1,0].Z; i < cells[1,1,2].Z+WorldMap.CELL_DEPTH; i++)
            {
                t2 = t;
                t = getTileFromCells(abs_x, abs_y, i).Name;
                if (t == "Air" && t2 != "Air")
                    return i;
            }

            return -1;*/
        }

        public int DropObject(int abs_x, int abs_y, int curr_z)
        {
            string t_cur = "", t_above;

            for (int i = curr_z + 1; i > cells[1, 1, 0].Z; i--)
            {
                t_above = t_cur;
                t_cur = getTileFromCells(abs_x, abs_y, i).Name;
                if (t_cur != "Air" && t_above == "Air")
                    return i+1;
            }
            return -1;
        }

        public void AddCreature(Creature c)
        {
            //Check if creature is within one of the loaded cells
            int[] cell_coords = getInternalCellPosFromID(wm.GetCellIDFromCoordinates(c.X, c.Y, c.Z));

            if (cell_coords == null)
                return;
                //throw new Exception("Error while trying to add creature: Creature position is not within loaded Map Cells!");

            CreatureList.Add(c.GUID, c);
            CreaturesByDistance.Add(c.GUID, Util.CalculateDistance(Player, c));
        }

        public void AddItem(Item i)
        {
            //Check if item is within one of the loaded cells
            int[] cell_coords = getInternalCellPosFromID(wm.GetCellIDFromCoordinates(i.X, i.Y, i.Z));

            if (cell_coords == null)
                return;
                //throw new Exception("Error while trying to add item: Item position is not within loaded Map Cells!");

            ItemList.Add(i.GUID, i);
        }

        public bool IsMovementPossible(int abs_x, int abs_y, int abs_z)
        {
            if (getInternalCellPosFromID(wm.GetCellIDFromCoordinates(abs_x, abs_y, abs_z)) == null)
                return false;

            Tile tar = getTileFromCells(abs_x, abs_y, abs_z);
            //Tile tar = getTileFromCells(abs_x, abs_y, DropObject(abs_x, abs_y, abs_z));
            if (tar == null)
            {
                Debug.WriteLine("Movement denied: Tile nonexistant.");
                return false;
            }


            //If target Tile blocks movement, deny movement
            if (tar.BlocksMovement)
            {
                Debug.WriteLine("Movement denied: Tile blocks movement.");
                return false;
            }


            //If target Tile is occupied by another creature, deny movement (TODO: Attacking)
            foreach (KeyValuePair<string, Creature> kv in CreatureList)
            {
                if (kv.Value.X == abs_x && kv.Value.Y == abs_y && kv.Value.Z == abs_z)
                {
                    Debug.WriteLine("Movement denied: Tile occupied.");
                    return false;
                }

            }

            return true;
        }

        public bool IsMovementPossibleDrop(int abs_x, int abs_y, int abs_z)
        {
            return IsMovementPossible(abs_x, abs_y, DropObject(abs_x, abs_y, abs_z));
        }

        public void Tick()
        {
            foreach (Creature c in CreatureList.Values)
            {
                if (c.GetType() == typeof(AICreature))
                {
                    AICreature cx = (AICreature)c;
                    cx.Tick();
                }
            }

            foreach (Item i in ItemList.Values)
            {
                i.Tick();
            }
        }

        public bool Render(TCODConsole con, int con_x, int con_y, int width, int height) 
        {
            //Method will draw to rectangle from origin con_x, con_y on the console by taking data from the cells.
            //Viewport is always centered on player, except on the border of the worldmap itself.

            //TODO: Correct z-handling

            int top; //Y
            int left; //X
            int right;
            int bottom;

            top = Player.Y - (height / 2);
            bottom = top + height;

            left = Player.X - (width / 2);
            right = left + width;

            if (top >= bottom || left >= right)
                return false;

            if (top < 0)
            {
                bottom -= top; //Bottom - Top (which is negative): new Bottom (10-(-5) = 15)
                top = 0;
            }

            if (bottom > WorldMap.GLOBAL_HEIGHT)
            {
                top -= (bottom - WorldMap.GLOBAL_HEIGHT); //bottom = 15, Globalheight = 10, Top = 5; => Top = 5 - (15-10) = 0
                bottom = WorldMap.GLOBAL_HEIGHT;
            }

            if (left < 0)
            {
                right -= left;
                left = 0;
            }

            if (right > WorldMap.GLOBAL_WIDTH)
            {
                left -= (right - WorldMap.GLOBAL_WIDTH);
                right = WorldMap.GLOBAL_WIDTH;
            }

            //Render the map
            int abs_x, abs_y, abs_z;
            Tile t;
            
            int curr_z = Player.Z;
            int z_view_dist = Map.VIEW_DISTANCE_TILES_Z;
            String displ_string = " ";

            Stopwatch sw = new Stopwatch();

            int debug_prints = 0;
            
            //world.GetTileFromID(tileMap[abs_x - X, abs_y - Y, abs_z - Z])
            byte[, ,] tilearr = new byte[right - left +1, bottom - top +1, (curr_z + z_view_dist) - (curr_z - z_view_dist) +1];

            sw.Start();
            for (abs_x = left; abs_x < right; abs_x++)
            {
                for (abs_y = top; abs_y < bottom; abs_y++)
                {
                    for (abs_z = curr_z - z_view_dist; abs_z < curr_z + z_view_dist; abs_z++)
                    {
                        tilearr[abs_x - left, abs_y - top, abs_z - (curr_z - z_view_dist)] = getTileIDFromCells(abs_x, abs_y, abs_z);
                    }
                }
            }

            int rel_x, rel_y, rel_z;
            bool above_clear = false ;
            for (abs_x = left; abs_x < right; abs_x++)
            {
                for (abs_y = top; abs_y < bottom; abs_y++)
                {
                    for (abs_z = curr_z - z_view_dist; abs_z < curr_z + z_view_dist; abs_z++)
                    {
                        rel_x = abs_x - left;
                        rel_y = abs_y - top;
                        rel_z = abs_z - (curr_z - z_view_dist);
                        //If current Tile is Air, skip ahead
                        if (tilearr[rel_x, rel_y, rel_z] == 0) //Air Tile
                            continue;
                        //Check if Tile above current is Air
                        if (tilearr[rel_x, rel_y, rel_z + 1] == 0)
                        {
                            t = wm.GetTileFromID(tilearr[rel_x, rel_y, rel_z]);

                            //If yes, draw
                            con.setBackgroundFlag(TCODBackgroundFlag.Default);
                            con.setForegroundColor(t.ForeColor);
                            displ_string = t.DisplayString;

                            //Is iteration z-level pointing at the tile BELOW the player level (curr_z)?
                            //Yes:

                            if (abs_z == curr_z - 1)
                            {
                                if (t.BackColor != null)
                                {
                                    con.setBackgroundColor(t.BackColor);
                                    con.setBackgroundFlag(TCODBackgroundFlag.Set);
                                }
                                debug_prints++;
                                con.print(con_x + (abs_x - left), con_y + (abs_y - top), displ_string);
                                break;
                            }

                            //No: Different z-level, set colors appropriately
                            con.setBackgroundFlag(TCODBackgroundFlag.Set);
                            
                            if (abs_z < curr_z - 1)
                            {
                                //displ_string = ".";

                                con.setForegroundColor(TCODColor.Interpolate(t.ForeColor, TCODColor.black, Math.Abs((abs_z-(curr_z-1))*(1.0f/z_view_dist))));
                                if (t.BackColor != null)
                                {
                                    con.setBackgroundColor(TCODColor.Interpolate(t.BackColor, TCODColor.black, Math.Abs((abs_z - (curr_z - 1)) * (1.0f / z_view_dist))));
                                }
                                else
                                {
                                    con.setBackgroundColor(TCODColor.black);
                                }
                            }
                            if (abs_z > curr_z - 1)
                            {
                                //displ_str = "#";

                                con.setForegroundColor(TCODColor.Interpolate(t.ForeColor, TCODColor.white, Math.Abs((abs_z - (curr_z - 1)) * (1.0f / z_view_dist))));
                                if (t.BackColor != null)
                                {
                                    con.setBackgroundColor(TCODColor.Interpolate(t.BackColor, TCODColor.white, Math.Abs((abs_z - (curr_z - 1)) * (1.0f / z_view_dist))));
                                }
                                else
                                {
                                    con.setBackgroundColor(TCODColor.black);
                                }
                            }

                            //Check surrounding tiles, make ramp if appropriate
                            // Ramp leading down a Z-Level
                            if (abs_z == curr_z - 2)
                            {
                                if ((rel_x > 0 && rel_y > 0) && (rel_x < (right - left - 1) && rel_y < (bottom - top - 1)))
                                {
                                    if (tilearr[rel_x + 1, rel_y, rel_z + 1] != 0 ||
                                        tilearr[rel_x - 1, rel_y, rel_z + 1] != 0 ||
                                        tilearr[rel_x, rel_y + 1, rel_z + 1] != 0 ||
                                        tilearr[rel_x, rel_y - 1, rel_z + 1] != 0)
                                    {
                                        con.setForegroundColor(t.ForeColor);
                                        displ_string = "v";
                                    }
                                }
                            }

                            // Ramp leading up a Z-Level
                            if (abs_z == curr_z)
                            {
                                if ((rel_x > 0 && rel_y > 0) && (rel_x < (right - left - 1) && rel_y < (bottom - top - 1)))
                                {
                                    if (tilearr[rel_x + 1, rel_y, rel_z  ] == 0 ||
                                        tilearr[rel_x - 1, rel_y, rel_z] == 0 ||
                                        tilearr[rel_x, rel_y + 1, rel_z ] == 0 ||
                                        tilearr[rel_x, rel_y - 1, rel_z ] == 0)
                                    {
                                        con.setForegroundColor(t.ForeColor);
                                        displ_string = "^";
                                    }
                                }
                            }

                            debug_prints++;
                            con.print(con_x + (abs_x - left), con_y + (abs_y - top), displ_string);
                            break;
                        }
                    }
                }
            }
            sw.Stop();

            //_out.SendMessage("Drew frame, printed " + debug_prints + " tiles, took " + sw.ElapsedMilliseconds + "ms.");

            //Render the player
            con.setBackgroundFlag(TCODBackgroundFlag.Default);
            con.setForegroundColor(Player.ForeColor);

            con.print(con_x + (Player.X - left), con_y + (Player.Y - top), Player.DisplayString);

            //Render the creatures
            foreach (Creature c in CreatureList.Values)
            {
                if (c.Z >= curr_z - Map.VIEW_DISTANCE_CREATURES_DOWN_Z && c.Z <= curr_z + Map.VIEW_DISTANCE_CREATURES_UP_Z)
                {
                    if (c.Z == curr_z - 1)
                        con.setForegroundColor(c.ForeColor);

                    if (c.Z < curr_z - 1)
                    {
                        con.setForegroundColor(TCODColor.Interpolate(c.ForeColor, TCODColor.black, Math.Abs((c.Z - (curr_z - 1)) * (1.0f / z_view_dist))));
                    }
                    if (c.Z > curr_z - 1)
                    {
                        con.setForegroundColor(TCODColor.Interpolate(c.ForeColor, TCODColor.white, Math.Abs((c.Z - (curr_z - 1)) * (1.0f / z_view_dist))));
                    }

                    con.print(con_x + (c.X - left), con_y + (c.Y - top), c.DisplayString);
                }
            }

            foreach (Item i in ItemList.Values)
            {
                if (i.Z >= curr_z - Map.VIEW_DISTANCE_CREATURES_DOWN_Z && i.Z <= curr_z + Map.VIEW_DISTANCE_CREATURES_UP_Z)
                {
                    if (i.Z == curr_z - 1)
                        con.setForegroundColor(i.ForeColor);

                    if (i.Z < curr_z - 1)
                    {
                        con.setForegroundColor(TCODColor.Interpolate(i.ForeColor, TCODColor.black, Math.Abs((i.Z - (curr_z - 1)) * (1.0f / z_view_dist))));
                    }
                    if (i.Z > curr_z - 1)
                    {
                        con.setForegroundColor(TCODColor.Interpolate(i.ForeColor, TCODColor.white, Math.Abs((i.Z - (curr_z - 1)) * (1.0f / z_view_dist))));
                    }

                    con.print(con_x + (i.X - left), con_y + (i.Y - top), i.DisplayString);
                }
            }

            return true;
        }



    }
}
