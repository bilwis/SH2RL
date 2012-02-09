using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;
using System.Diagnostics;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;

using ShootyShootyRL.Objects;

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

namespace ShootyShootyRL.Mapping
{
    /// <summary>
    /// This object represents and works with the map as it resides in memory. It consists of 3x3x3 cells and consitutes a chunk of the world map.
    /// </summary>
    public class Map
    {
        private static bool DEBUG_OUTPUT = true;

        object cellLock = new object();
        Cell[, ,] cells;
        bool[, ,] loaded;
        bool[, ,] load_diff;

        WorldMap wm;
        public Creature Player;
        public Dictionary<String, Creature> CreatureList;
        public Dictionary<String, double> CreaturesByDistance;
        public Dictionary<String, Item> ItemList;

        public bool initialized = false;

        public static int VIEW_DISTANCE_TILES_Z = 10;
        public static int VIEW_DISTANCE_CREATURES_DOWN_Z = 3;
        public static int VIEW_DISTANCE_CREATURES_UP_Z = 1;

        private int currentCellId;

        private MessageHandler _out;
        private FactionManager facman; 
        private SQLiteConnection dbconn;


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
            centerAndLoad(player);
        }

        /// <summary>
        /// This function assigns the central cell to the cell in which the player currently resides and loads all surrounding
        /// cells into memory.
        /// </summary>
        private void centerAndLoad(Creature player)
        {
            cells[1, 1, 1] = wm.GetCellFromCoordinates(player.X, player.Y, player.Z);
            cells[1, 1, 1].Load();
            currentCellId = cells[1, 1, 1].CellID;

            loaded = new bool[3, 3, 3];
            loaded[1, 1, 1] = true;

            load_diff = new bool[3, 3, 3];
            load_diff[1, 1, 1] = true;

            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    for (int z = -1; z < 2; z++)
                    {
                        if (!(x == 0 && y == 0 && z == 0))
                        {
                            cells[1 + x, 1 + y, 1 + z] = wm.GetAdjacentCell(x, y, z, cells[1, 1, 1]);

                            if (cells[1 + x, 1 + y, 1 + z] == null)
                                throw new Exception("Player in border cell!");

                            if (Program.game.MULTITHREADED_LOADING)
                            {
                                load_diff[1 + x, 1 + y, 1 + z] = true;

                                System.ComponentModel.BackgroundWorker bw = new System.ComponentModel.BackgroundWorker();
                                bw.DoWork += new System.ComponentModel.DoWorkEventHandler(backgroundWorker_DoWork);
                                bw.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);

                                bw.RunWorkerAsync(cells[1 + x, 1 + y, 1 + z]);
                            }
                            else
                            {
                                cells[1+ x, 1+  y, 1+ z].Load();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This function handles the map side of player movement. It will check on the viability
        /// of the move and, if necessary, load new Cells and dispose the old ones.
        /// </summary>
        /// <returns>true if the movement is possible, false otherwise</returns>
        public bool CheckPlayerMovement(int abs_x, int abs_y, int abs_z)
        {

            int cell_x = -1;
            int cell_y = -1;
            int cell_z = -1;
            int[] cell_coords = new int[3];
            int relCellID;

            //Get the Cell ID of the destination cell.
            relCellID = wm.GetCellIDFromCoordinates(abs_x, abs_y, abs_z);

            //Check for movement blocking tiles or other creatures-
            // also simulate the player dropping from the destination coordinates
            // to ensure the cell in which he would actually end up in (seeing as
            // he can't fly) is checked.
            if (!IsMovementPossibleDrop(abs_x, abs_y, abs_z))
                return false;

            //Not the same cell anymore? Better load the new ones!
            if (relCellID != currentCellId)
            {
                //If the map cells haven't been completely loaded/streamed yet, deny movement
                if (!initialized)
                    return false;

                //Get coordinates of the now player-holding cell relative to the previous.
                //NOTE: This also limits the movement. If the player is to be "teleported" to a
                //location more than one cell's distance away, a different function must be used (TODO).
                cell_coords = getInternalCellPosFromID(relCellID);
                if (cell_coords == null)
                    throw new Exception("Error while trying to move player: New cell was not found in Map Cell array!");

                cell_x = cell_coords[0];
                cell_y = cell_coords[1];
                cell_z = cell_coords[2];

                //Try and shift the cells (including loading the new ones)
                //NOTE: This function will return false if the player tries to move into the cells on the 
                //border of the world map. That is because it can't load all surrounding cells on the border 
                //(seeing as some don't exist). Handling those special cases where the number of loaded cells
                //is less than 12 seems more work than it's worth. 
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

            initialized = false;

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

                                loaded[x, y, z] = false;
                                load_diff[x, y, z] = true;
                                cells[x, y, z] = wm.GetAdjacentCell(shift_vect[0], shift_vect[1], shift_vect[2], tempcells[x, y, z]);
                                if (Program.game.MULTITHREADED_LOADING)
                                {
                                    System.ComponentModel.BackgroundWorker bw = new System.ComponentModel.BackgroundWorker();
                                    bw.DoWork += new System.ComponentModel.DoWorkEventHandler(backgroundWorker_DoWork);
                                    bw.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);

                                    bw.RunWorkerAsync(cells[ x, y, z]);
                                }
                                else
                                {
                                    cells[x,y ,z].Load();
                                }
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

        private void backgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            Cell c = (Cell)e.Argument;
            c.Load();
            e.Result = c.CellID;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            int[] coords = getInternalCellPosFromID((int)e.Result);
            loaded[coords[0], coords[1], coords[2]] = true;
            Console.WriteLine("Cell #" + (int)e.Result + " loaded! Stopping Thread.");
            if (checkAllLoaded())
            {
                loadDifferences();
                initialized = true;
            }
        }

        private bool checkAllLoaded()
        {
            foreach (bool b in loaded)
            {
                if (!b)
                    return false;
            }

            return true;
        }

        private void loadDifferences()
        {
            List<int> cell_diff = new List<int>();
            Dictionary<int, Cell> cell_dict = new Dictionary<int, Cell>();

            //Retrieve list of cell ids for which differences need to be loaded
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        if (load_diff[x, y, z])
                        {
                            cell_diff.Add(cells[x, y, z].CellID);
                            cell_dict.Add(cells[x, y, z].CellID, cells[x, y, z]);
                        }
                    }
                }
            }

            String whereClause = "";
            int[] ids = cell_diff.ToArray<int>();

            for (int i = 0; i < ids.Length; i++)
            {
                whereClause += "cell_id='"+ ids[i] +"'";
                if (i != ids.Length - 1)
                    whereClause += " or ";
            }

            /*
            command.CommandText = "CREATE TABLE IF NOT EXISTS diff_map (cell_id BLOB NOT NULL, abs_x BLOB, abs_y BLOB, abs_z BLOB, tile BLOB);";
            command.ExecuteNonQuery();
             */

            SQLiteCommand command = new SQLiteCommand(dbconn);
            SQLiteDataReader reader;

            command.CommandText = "SELECT * FROM diff_map WHERE " + whereClause;
            reader = command.ExecuteReader();

            while (reader.Read())
            {
                cell_dict[Convert.ToInt32(Util.ByteArrayToString((byte[])reader[0]))].SetTile(
                    Convert.ToInt32(Util.ByteArrayToString((byte[])reader[1])),
                    Convert.ToInt32(Util.ByteArrayToString((byte[])reader[2])),
                    Convert.ToInt32(Util.ByteArrayToString((byte[])reader[3])),
                    Convert.ToUInt16(Util.ByteArrayToString((byte[])reader[4])));
            }

            reader.Close();
            reader.Dispose();
            command.Dispose();
            load_diff = new bool[3, 3, 3];
        }

        /// <summary>
        /// This function deserializes (loads) an item from the item database.
        /// </summary>
        /// <param name="guid">The guid of the item to deserialize.</param>
        public bool LoadItem(string guid)
        {
            //The item's data is retrieved from the DB using the GUID.
            //The "data" column is an ASCII-Encoded byte array which is then
            //converted into a string which is then converted into an "hex-encoded"
            //or proper byte-array (Oh god why?). The resulting byte-array is then written into
            //a MemoryStream from which the Item is deserialized.

            byte[] data;
            int color;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter deserializer = new BinaryFormatter();

            SQLiteDataReader reader;
            //Check if an item with the given GUID is in the data base. If not, exit.
            if (!checkIsInDatabase(Util.DBLookupType.Item, guid, out reader))
                return false;
            
            //DB Results have the following format:
            // reader[0] = GUID
            // reader[1] = the data array
            // reader[2] = a byte array holding an Int32 object holding three bytes encoding the color of the object

            //Parse this pesky string of ASCII encoded bytes into an actual proper hex-encoded byte array.
            data = Util.ConvertDBByteArray((byte[])reader[1]);

            color = Convert.ToInt32(Util.ByteArrayToString((byte[])reader[2]));
            
            //Write all the data from the freshly parsed byte array into the MemoryStream
            fstream.Write(data, 0, data.Length);

            //Reset the MemoryStream
            fstream.Seek(0, SeekOrigin.Begin);

            //Extract (deserialize) the delicious item from the MemoryStream
            Item i = (Item)deserializer.Deserialize(fstream);

            //And initialize the new and shining item
            i.Init(new TCODColor(color >> 16, color >> 8 & 0xFF, color & 0xFF), _out);

            //Add the initialized item to the maps dictionary of items
            AddItem(i);

            //Cleanup
            reader.Close();
            reader.Dispose();
            fstream.Close();

            //Done!
            return true;
        }

        /// <summary>
        /// This function serializes (saves) an item to the item database.
        /// </summary>
        /// <param name="guid">The guid of the item to serialize.</param>
        public void UnloadItem(string guid)
        {
            //The item is retrieved using the given guid, then serialized to the 
            //MemoryStream which is then converted into a string which is then 
            //written into the database. For the DB the MemoryStream (which holds a byte array) 
            //is converted into a string (after the pattern: [1A, 20, 00, FF] -> 1A2000FF)
            byte[] arr;
            string data;
            int color;
            bool update = false;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter serializer = new BinaryFormatter();
            SQLiteCommand command = new SQLiteCommand(dbconn);

            //Check if item already is in DB, if so set to update instead of insert
            if (checkIsInDatabase(Util.DBLookupType.Item, guid))
                update = true;

            //Begin a new SQLiteTransaction 
            //NOTE: A SQL Transaction stores several SQL commands
            //before commiting them to the database. This can save a
            //lot of time.
            SQLiteTransaction tr = dbconn.BeginTransaction();
            command.Transaction = tr;

            Item i = ItemList[guid];

            //Call the save function of the item/object which "uninitializes" it.
            i.Save();

            //Do the actual serialization into the MemoryStream
            serializer.Serialize(fstream, i);

            arr = new byte[fstream.Length];

            //Encode the item's ForeColor into a single integer
            //NOTE: The "technique" used is called bit shift.
            color = i.ForeColor.Red << 16 | i.ForeColor.Green << 8 | i.ForeColor.Blue;

            //Reset and read the MemoryStream into the byte array.
            fstream.Seek(0, SeekOrigin.Begin);
            fstream.Read(arr, 0, (int)fstream.Length);

            //Convert the hex-encoded byte array extracted from the serialized MemoryStream
            //into an ascii-encoded string (and remove all dashes).
            data = BitConverter.ToString(arr).Replace("-", string.Empty);

            //Prepare the actual SQL command (UPDATE if guid already exists, INSERT if not)
            //NOTE: Please refer to SQL documentation for detailed information.
            if (!update)
                command.CommandText = "INSERT INTO items (guid, data, color) VALUES ('" + guid + "', '" + data + "', " + color + ")";
            if (update)
                command.CommandText = "UPDATE items SET data='" + data + "', color=" + color + " WHERE guid='" + guid + "'";

            //Execute and commit SQLite command and transaction.
            command.ExecuteNonQuery();
            tr.Commit();
            
            //Remove the saved item from the maps ItemList.
            ItemList.Remove(guid);

            //Cleanup
            command.Dispose();
            tr.Dispose();
            fstream.Close();
        }

        /// <summary>
        /// This function deserializes (loads) a creature with an attached AI from the creature database.
        /// </summary>
        /// <param name="guid">The guid of the creature to deserialize.</param>
        public bool LoadAICreature(string guid)
        {
            //The creatures's data is retrieved from the DB using the GUID.
            //The "data" column is an ASCII-Encoded byte array which is then
            //converted into a string which is then converted into an "hex-encoded"
            //or proper byte-array (Oh god why?). The resulting byte-array is then written into
            //a MemoryStream from which the Item is deserialized.
            AICreature c;
            Faction _fac;
            AI _ai;

            byte[] data;
            string fac_id, ai_id;
            int color;

            bool faction_deserialized = false;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter deserializer = new BinaryFormatter();
            SQLiteDataReader reader;

            //DESERIALIZE AI CREATURE
            //Check if in database
            if (!checkIsInDatabase(Util.DBLookupType.AICreature, guid, out reader))
                throw new Exception("Error while trying to load AICreature: No AICreature entry with guid " + guid + " found.");

            //DB Results have the following format:
            // reader[0] = GUID
            // reader[1] = the data array
            // reader[2] = the GUID of the attached faction
            // reader[3] = the GUID of the attached AI
            // reader[4] = a byte array holding an Int32 object holding three bytes encoding the color of the object

            //Parse ASCII-Encoded byte array to Hex-Encoded byte array
            data = Util.ConvertDBByteArray((byte[])reader[1]);

            fac_id = Util.ByteArrayToString((byte[])reader[2]);
            ai_id = Util.ByteArrayToString((byte[])reader[3]);
            color = Convert.ToInt32(Util.ByteArrayToString((byte[])reader[4]));

            //Write the converted byte array into the MemoryStream (and reset it to origin)
            fstream.Write(data, 0, data.Length);
            fstream.Seek(0, SeekOrigin.Begin);

            //Deserialize the creature object itself
            c = (AICreature)deserializer.Deserialize(fstream);

            //DESERIALIZE OR LOAD FACTION
            //If the faction with the GUID attached to the deserialized creature
            //is already registered in the maps faction manager, then there is no need to load it again.
            _fac = facman.GetFaction(fac_id); 
            if (_fac == null)
            {
                //Clear the SQLite Reader
                reader.Dispose();

                //Check if in database
                if (!checkIsInDatabase(Util.DBLookupType.Faction, fac_id, out reader))
                    throw new Exception("Error while trying to load AICreature: No Faction entry with guid " + fac_id + " found."); ;

                //DB Results have the following format:
                // reader[0] = GUID
                // reader[1] = the data array

                //Parse ASCII-Encoded byte array to Hex-Encoded byte array
                data = Util.ConvertDBByteArray((byte[])reader[1]);

                //Clear (and reset) the MemoryStream
                fstream.SetLength(0);
                fstream.Seek(0, SeekOrigin.Begin);

                //Write the converted byte array into the MemoryStream (and reset it to origin)
                fstream.Write(data, 0, data.Length);
                fstream.Seek(0, SeekOrigin.Begin);

                //Deserialize the faction object itself
                _fac = (Faction)deserializer.Deserialize(fstream);
                faction_deserialized = true;
            }

            //DESERIALIZE AI
            if (!checkIsInDatabase(Util.DBLookupType.AI, ai_id, out reader))
                throw new Exception("Error while trying to load AICreature: No AI entry with guid " + ai_id + " found."); 

            //DB Results have the following format:
            // reader[0] = GUID
            // reader[1] = the data array

            //Parse ASCII-Encoded byte array to Hex-Encoded byte array
            data = Util.ConvertDBByteArray((byte[])reader[1]);

            //Clear (and reset) the MemoryStream
            fstream.SetLength(0);
            fstream.Seek(0, SeekOrigin.Begin);

            //Write the converted byte array into the MemoryStream (and reset it to origin)
            fstream.Write(data, 0, data.Length);
            fstream.Seek(0, SeekOrigin.Begin);

            //Deserialize the ai object itself
            _ai = (AI)deserializer.Deserialize(fstream);

            //INITIALIZE OBJECTS
            //If the faction was loaded from DB, initialize it to the faction manager
            if (faction_deserialized)
                _fac.Init(facman);

            //Initialize the AICreature
            c.Init(new TCODColor(color >> 16, color >> 8 & 0xFF, color & 0xFF), _out, _fac, new Objects.Action(ActionType.Idle, null, c, 0.0d), _ai, this);

            //Add the newly initialized AICreature to the map creature dictionaries
            AddCreature(c);
            
            //CLEANUP
            reader.Dispose();
            reader.Close();

            fstream.Close();

            //Done!
            return true;
        }

        /// <summary>
        /// This function serializes (saves) a creature to the creature database.
        /// </summary>
        /// <param name="guid">The guid of the creature to serialize.</param>
        public bool UnloadAICreature(string guid)
        {
            //The creature is retrieved using the given guid, then serialized to the 
            //MemoryStream which is then converted into a string which is then 
            //written into the database. For the DB the MemoryStream (which holds a byte array) 
            //is converted into a string (after the pattern: [1A, 20, 00, FF] -> 1A2000FF)
            AICreature c;

            byte[] arr;
            string data;
            int color;

            bool update = false;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter serializer = new BinaryFormatter();
            SQLiteCommand command = new SQLiteCommand(dbconn);

            //SERIALIZE AICREATURE

            //Check if creature actually is an AICreature
            if (CreatureList[guid].GetType() != typeof(AICreature))
                return false;

            //Check if creature already is in DB
            if (checkIsInDatabase(Util.DBLookupType.AICreature, guid))
                update = true;

            //Initialize Transaction for quick handling of several queries.
            SQLiteTransaction tr = dbconn.BeginTransaction();
            command.Transaction = tr;

            //Retrieve the AICreature with the given GUID and call it's save function
            c = (AICreature)CreatureList[guid];
            c.Save();

            //Serialize the creature into the MemoryStream
            serializer.Serialize(fstream, c);

            arr = new byte[fstream.Length];

            //Reset and read the stream into a byte array
            fstream.Seek(0, SeekOrigin.Begin);
            fstream.Read(arr, 0, (int)fstream.Length);

            //Convert the hex-encoded byte array extracted from the serialized MemoryStream
            //into an ascii-encoded string (and remove all dashes).
            data = BitConverter.ToString(arr).Replace("-", string.Empty);

            //Encode the item's ForeColor into a single integer
            //NOTE: The "technique" used is called bit shift.
            color = c.ForeColor.Red << 16 | c.ForeColor.Green << 8 | c.ForeColor.Blue;

            //Prepare and execute query to insert/update creature
            if (!update)
                command.CommandText = "INSERT INTO ai_creatures (guid, data, faction_id, ai_id, color) VALUES ('" + guid + "', '" + data + "', '" +  c.Faction.GUID + "', '" + c.AI.GUID + "', " + color + ")";
            if (update)
                command.CommandText = "UPDATE ai_creatures SET data='" + data + "', faction_id='" +  c.Faction.GUID + "', ai_id='" + c.AI.GUID + "', color=" + color + " WHERE guid='" + guid + "'";
            
            //Execute the command
            command.ExecuteNonQuery();

            //SERIALIZE AI
            //Check if the AI is already in DB
            update = false;
            if (checkIsInDatabase(Util.DBLookupType.AI, c.AI.GUID))
                update = true;

            //Retrieve and save the AI associated with the AICreature
            AI _ai = c.AI;
            _ai.Save();

            //blah blah see above
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

            //SERIALIZE FACTION
            //And the same again...
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

            //CLEANUP
            CreatureList.Remove(guid);
            CreaturesByDistance.Remove(guid);

            //done!
            command.Dispose();
            fstream.Close();
            return true;
        }

        /// <summary>
        /// This function checks if an item with the given GUID is in the object DB and returns the result of the lookup 
        /// in an SQLiteDataReader object.
        /// </summary>
        /// <param name="lookup"></param>
        /// <param name="guid"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
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

        /// <summary>
        /// This function checks if an item with the given GUID is in the object DB.
        /// </summary>
        /// <param name="lookup"></param>
        /// <param name="guid"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private bool checkIsInDatabase(Util.DBLookupType lookup, string guid)
        {
            SQLiteDataReader derp;
            return checkIsInDatabase(lookup, guid, out derp);
        }

        /// <summary>
        /// This function iterates through the content of a cell and unloads every object into DB.
        /// </summary>
        /// <param name="c">The cell to be unloaded.</param>
        private void unloadCellContent(Cell c)
        {
            SQLiteCommand command = new SQLiteCommand(dbconn);

            //Clear all exisiting cell content from DB
            command.CommandText = "DELETE FROM cell_contents WHERE cell_id='" + c.CellID + "'";
            command.ExecuteNonQuery();

            //Setup Transaction
            SQLiteTransaction tr = dbconn.BeginTransaction();
            command.Transaction = tr;

            //Save all items by retrieving all guids and iterating through
            //them. (Can't iterate through an IEnumerable and change it within the iteration!)
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

            //Save all creatures (refer item saving above)
            keys = new string[CreatureList.Count];
            keys = CreatureList.Keys.ToArray<string>();

            for (int i = 0; i < keys.Length; i++)
            {
                Creature cr = CreatureList[keys[i]];
                //Only save AICreatures though (only "Creature" should be the player itself)
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

            //Commit, cleanup
            tr.Commit();
            command.Dispose();
        }

        /// <summary>
        /// This function loads all content associated with the given cell ID from the database.
        /// </summary>
        /// <param name="id">The CellID of the cell whose content is to be loaded.</param>
        private void loadCellContent(int id)
        {
            SQLiteCommand command = new SQLiteCommand(dbconn);

            //Retrieve all content GUIDs for the given cell ID
            command.CommandText = "SELECT * FROM cell_contents WHERE cell_id='" + id + "'";
            SQLiteDataReader reader = command.ExecuteReader();

            //DB Results have the following format:
            // reader[0] = cell_id
            // reader[1] = a byte array encoding an integer encoding an Util.DBLookupType
            // reader[2] = the GUID of the content object

            //Load them objects
            int item_type;
            string content_guid;

            while (reader.Read())
            {
                //Get the GUID of the content object
                content_guid = Util.ByteArrayToString((byte[])reader[2]);

                //Determine which type of content it is and call the appropriate function
                item_type = Int32.Parse(Util.ByteArrayToString((byte[])reader[1]));
                if (item_type == (int)Util.DBLookupType.Item)
                    LoadItem(content_guid);
                if (item_type == (int)Util.DBLookupType.AICreature)
                    LoadAICreature(content_guid);
            }

            //Cleanup
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
            //TODO: I doubt that this is particularily performance-eating a function
            //but there's potential to optimize here: Create a dictionary holding the
            //cellId as key and the coordinates as value (perhaps "bitshift-encoded")

            int[] coord = new int[3];

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        //Gotcha, bietch! 
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

            //No Cell with the given Id was found!
            return null;
        }

        /// <summary>
        /// This function retrieves the ID of the tile at the given location.
        /// </summary>
        /// <returns>A byte representing the tile at the given location or 0 if the cell holding the tile is not loaded.</returns>
        private ushort getTileIDFromCells(int abs_x, int abs_y, int abs_z)
        {
            int x = 0, y = 0, z = 0;
            
            //Check if coordinates are within loaded map area
            if (!isCoordinateLoaded(abs_x, abs_y, abs_z))
                return 0;

            x = (int)((abs_x - cells[0, 0, 0].X) / WorldMap.CELL_WIDTH);
            y = (int)((abs_y - cells[0, 0, 0].Y) / WorldMap.CELL_HEIGHT);
            z = (int)((abs_z - cells[0, 0, 0].Z) / WorldMap.CELL_DEPTH);

            return cells[x, y, z].GetTileID(abs_x, abs_y, abs_z);

        }

        /// <summary>
        /// This function retrieves the Tile object stored in the map at the given location.
        /// </summary>
        /// <returns>A Tile object or null if the cell holding the tile is not loaded.</returns>
        private Tile getTileFromCells(int abs_x, int abs_y, int abs_z)
        {
            int x = 0, y = 0, z = 0;

            //Check if coordinates are within loaded map area
            if (!isCoordinateLoaded(abs_x, abs_y, abs_z))
                return null;

            x = (int)((abs_x - cells[0, 0, 0].X) / WorldMap.CELL_WIDTH);
            y = (int)((abs_y - cells[0, 0, 0].Y) / WorldMap.CELL_HEIGHT);
            z = (int)((abs_z - cells[0, 0, 0].Z) / WorldMap.CELL_DEPTH);

            return cells[x, y, z].GetTile(abs_x, abs_y, abs_z);
                     
        }

        /// <summary>
        /// This function checks wheter the given coordinates are within the bounds of the loaded cells.
        /// </summary>
        /// <returns>True, if the coordinate point at a loaded tile, False if not.</returns>
        private bool isCoordinateLoaded(int abs_x, int abs_y, int abs_z)
        {
            if (abs_x < cells[0, 0, 0].X || abs_x > cells[2, 0, 0].X + WorldMap.CELL_WIDTH)
                return false;
            if (abs_y < cells[0, 0, 0].Y || abs_y > cells[0, 2, 0].Y + WorldMap.CELL_HEIGHT)
                return false;
            if (abs_z < cells[0, 0, 0].Z || abs_z > cells[0, 0, 2].Z + WorldMap.CELL_DEPTH)
                return false;

            return true;
        }
        
        [Obsolete("Use DropObject instead, this function only checks from the topmost tile")]
        public int GetGround(int abs_x, int abs_y)
        {
            string t = "Air", t2;

            for (int i = cells[1, 1, 2].Z + WorldMap.CELL_DEPTH - 1; i > cells[1, 1, 0].Z; i--)
            {
                t2 = t;
                t = getTileFromCells(abs_x, abs_y, i).Name;
                if (t != "Air" && t2 == "Air")
                    return i +1;
            }

            return -1;
        }

        /// <summary>
        /// This function determines the z-coordinate of object if dropped at the given location.
        /// </summary>
        /// <param name="curr_z">The z-level from which to drop the object.</param>
        /// <returns>The z-level at which the object will rest when dropped or -1 if no ground was hit.</returns>
        public int DropObject(int abs_x, int abs_y, int curr_z)
        {
            string t_cur = "", t_above;

            //Work from curr_z+1* to the bottom:
            // Assign t_above the previous Tile, fetch the current tile in t_cur
            // Check if t_cur is NOT air AND t_above IS air (ground was hit)
            //
            //NOTE: This function returns not the z-level of the actual ground tile
            //but the z-level above (due to the way that object OCCUPY the tile they
            //are located in, they have to STAND ON TOP of the ground tile).
            //
            //IMPORTANT: * why curr_z+1? Creatures can negotiate z-level changes of +1!

            for (int i = curr_z + 1; i > cells[1, 1, 0].Z; i--)
            {
                t_above = t_cur;
                t_cur = getTileFromCells(abs_x, abs_y, i).Name;
                if (t_cur != "Air" && t_above == "Air")
                    return i+1;
            }
            return -1;
        }

        /// <summary>
        /// This function will register the given creature with the maps creature dictionaries.
        /// </summary>
        public bool AddCreature(Creature c)
        {
            //Check if creature is within one of the loaded cells
            if (isCoordinateLoaded(c.X, c.Y, c.Z) == null)
                return false;

            //Add to dictionaries
            CreatureList.Add(c.GUID, c);
            CreaturesByDistance.Add(c.GUID, Util.CalculateDistance(Player, c));
            return true;
        }

        /// <summary>
        /// This function will register the given item with the maps item dictionary.
        /// </summary>
        public bool AddItem(Item i)
        {
            //Check if item is within one of the loaded cells
            if (isCoordinateLoaded(i.X, i.Y, i.Z) == null)
                return false;

            ItemList.Add(i.GUID, i);
            return true;
        }

        /// <summary>
        /// This function checks wheter a creature can legally move to the given position.
        /// </summary>
        public bool IsMovementPossible(int abs_x, int abs_y, int abs_z)
        {
            //Check if target coordinates are within loaded cells
            if (isCoordinateLoaded(abs_x, abs_y, abs_z) == null)
                return false;

            //Check wheter Tile at target location exists (this should never be reached but
            // intercepted by the check above)
            Tile tar = getTileFromCells(abs_x, abs_y, abs_z);
            if (tar == null)
            {
                if (DEBUG_OUTPUT)
                    _out.SendDebugMessage("Movement denied: Tile nonexistant.");
                throw new Exception("FATAL ERROR while trying to check movement: Tile at " + abs_x + "," + abs_y + "," + abs_z + " does not exist.");
            }


            //If target Tile blocks movement, deny movement
            if (tar.BlocksMovement)
            {
                if (DEBUG_OUTPUT)
                    _out.SendDebugMessage("Movement denied: Tile blocks movement.");
                return false;
            }


            //If target Tile is occupied by another creature, deny movement (TODO: Attacking)
            foreach (KeyValuePair<string, Creature> kv in CreatureList)
            {
                if (kv.Value.X == abs_x && kv.Value.Y == abs_y && kv.Value.Z == abs_z)
                {
                    if (DEBUG_OUTPUT)
                        _out.SendDebugMessage("Movement denied: Tile occupied.");
                    return false;
                }

            }

            //Okay to go, sir!
            return true;
        }

        /// <summary>
        /// This function checks wheter a creature can legally move to the given position if it were dropped from the given curr_z to the ground beneath.
        /// </summary>
        public bool IsMovementPossibleDrop(int abs_x, int abs_y, int curr_z)
        {
            int z = DropObject(abs_x, abs_y, curr_z);
            if (z == -1)
                return false;
            return IsMovementPossible(abs_x, abs_y, z);
        }

        public void Tick()
        {
            //Tick all loaded creatures
            foreach (Creature c in CreatureList.Values)
            {
                if (c.GetType() == typeof(AICreature))
                {
                    AICreature cx = (AICreature)c;
                    cx.Tick();
                }
            }

            //Tick all loaded items
            foreach (Item i in ItemList.Values)
            {
                i.Tick();
            }
        }

        public bool Render(TCODConsole con, int con_x, int con_y, int width, int height) 
        {
            //This method is fairly convoluted because of all the intricacies of rendering ALL THE THINGS properly.
            //It could really use a makeover, but I'm not in the "OMGWTFBBQ MAJOR REWRITE UP IN THIS BIATCH" phase
            // and I'm afraid of breaking things.

            #region "Viewport setup"
            //In hnjah, the "viewport" is set up. The viewport is what the camera is in 3D games. 
            //It determines what needs to be rendered (everything not in the viewport on any of the three
            //axes is "culled", i.e. not rendered).
            //The viewport is ALWAYS centered on the player.

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
                bottom -= top; //Bottom - Top (which is negative): ex.: new Bottom (10-(-5) = 15)
                top = 0;
            }

            if (bottom > WorldMap.GLOBAL_HEIGHT)
            {
                top -= (bottom - WorldMap.GLOBAL_HEIGHT); //ex.: bottom = 15, Globalheight = 10, Top = 5; => Top = 5 - (15-10) = 0
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
            #endregion

            #region "Map rendering"

            int abs_x, abs_y, abs_z;
            int rel_x, rel_y, rel_z;
            Tile t;
            
            int curr_z = Player.Z;
            int z_view_dist = Map.VIEW_DISTANCE_TILES_Z;    //"Z viewing distance" is the maximum elevation change that
                                                            //is properly rendered out.
            String displ_string = " ";

            //Debug vars:
            Stopwatch sw = new Stopwatch();
            int debug_prints = 0;
            
            sw.Start();
            //AND THEY'RE OFF!

            //Buffer all tiles in the viewport into a three dimensional byte array
            ushort[, ,] tilearr = new ushort[right - left + 1, bottom - top + 1, (curr_z + z_view_dist) - (curr_z - z_view_dist) + 1];
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

            //Now go through all the tiles...
            for (abs_x = left; abs_x < right; abs_x++)
            {
                for (abs_y = top; abs_y < bottom; abs_y++)
                {
                    for (abs_z = curr_z - z_view_dist; abs_z < curr_z + z_view_dist; abs_z++)
                    {
                        //...determine their relative coordinates (relative to the upper left
                        // corner of the viewport *and the tile byte array*, that is)
                        rel_x = abs_x - left;
                        rel_y = abs_y - top;
                        rel_z = abs_z - (curr_z - z_view_dist);

                        //If current Tile is Air, skip ahead, because no hot rendering action is needed
                        if (tilearr[rel_x, rel_y, rel_z] == 0) //Air Tile
                            continue;

                        //If the current Tile is NOT Air, check if Tile ABOVE current is Air
                        // (Only then is it a candidate for rendering!)
                        if (tilearr[rel_x, rel_y, rel_z + 1] == 0)
                        {
                            //Retrieve the actual tile data
                            t = wm.GetTileFromID(tilearr[rel_x, rel_y, rel_z]);

                            //Prepare for render...
                            con.setBackgroundFlag(TCODBackgroundFlag.Default);
                            con.setForegroundColor(t.ForeColor);
                            displ_string = t.DisplayString;

                            //...but wait!
                            //Is iteration z-level pointing at a tile ONE BELOW the player level (curr_z)?
                            
                            if (abs_z == curr_z - 1)
                            {
                                //Yes! The tiles should be rendered normally!
                                if (t.BackColor != null)
                                {
                                    con.setBackgroundColor(t.BackColor);
                                    con.setBackgroundFlag(TCODBackgroundFlag.Set);
                                }
                                //DO IT!
                                debug_prints++;
                                con.print(con_x + (abs_x - left), con_y + (abs_y - top), displ_string);
                                break;
                            }

                            //No... Different z-level, set colors appropriately.
                            con.setBackgroundFlag(TCODBackgroundFlag.Set);
                            
                            if (abs_z < curr_z - 1)
                            {
                                //Tile is BELOW (one below) player level? Darken that biatch up!
                                //NOTE: Darkening/Interpolating with black is gradual, with a tile z_view_dist tiles below player level 
                                //being rendered completely black and a tile on player level completely normal.
                                con.setForegroundColor(TCODColor.Interpolate(t.ForeColor, TCODColor.black, Math.Abs((abs_z-(curr_z-1))*(1.0f/z_view_dist))));
                                if (t.BackColor != null)
                                {
                                    con.setBackgroundColor(TCODColor.Interpolate(t.BackColor, TCODColor.black, Math.Abs((abs_z - (curr_z - 1)) * (1.0f / z_view_dist))));
                                }
                                else
                                {
                                    //If tile doesn't have a background, fall back to black
                                    con.setBackgroundColor(TCODColor.black);
                                }
                            }
                            if (abs_z > curr_z - 1)
                            {
                                //Tile is ABOVE (one below) player level? Brighten that biatch up!
                                //NOTE: Brightening/Interpolating with white is gradual, with a tile z_view_dist tiles below player level 
                                //being rendered completely white and a tile on player level completely normal.
                                con.setForegroundColor(TCODColor.Interpolate(t.ForeColor, TCODColor.white, Math.Abs((abs_z - (curr_z - 1)) * (1.0f / z_view_dist))));
                                if (t.BackColor != null)
                                {
                                    con.setBackgroundColor(TCODColor.Interpolate(t.BackColor, TCODColor.white, Math.Abs((abs_z - (curr_z - 1)) * (1.0f / z_view_dist))));
                                }
                                else
                                {
                                    //If tile doesn't have a background, fall back to black
                                    con.setBackgroundColor(TCODColor.black);
                                }
                            }

                            //Check surrounding tiles, make a ramp if appropriate!
                            //NOTE: A ramp is rendered instead of the normal foreground char for
                            // this particular tile if one or more tiles neighboring it on the x- or y-
                            // axis are on the player level. (IMPORTANT: This doesn't actually change the nature
                            // of the tile, just it's display!)

                            // Ramp leading DOWN a Z-Level
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

                            // Ramp leading UP a Z-Level
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

                            //Print that tile for reals!
                            debug_prints++;
                            con.print(con_x + (abs_x - left), con_y + (abs_y - top), displ_string);
                            break;
                        }
                    }
                }
            }
            sw.Stop();
            #endregion

            //Report the time it took to render one frame! (but only if in Debug mode!)
            if (DEBUG_OUTPUT)
                _out.SendMessage("Drew frame, printed " + debug_prints + " tiles, took " + sw.ElapsedMilliseconds + "ms.");

            #region "Object rendering"
            //Render the player
            con.setBackgroundFlag(TCODBackgroundFlag.Default);
            con.setForegroundColor(Player.ForeColor);

            con.print(con_x + (Player.X - left), con_y + (Player.Y - top), Player.DisplayString);

            //Render the creatures
            //NOTE/TODO: Creatures are only visible in a certain range of height differences. Right now
            // it's hardcoded, but it will probably be dynamic with the addition of a 3D raycasting algorithm!
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

            //Render the items
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
            #endregion

            //DONE!
            return true;
        }



    }
}
