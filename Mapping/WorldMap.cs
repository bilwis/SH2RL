using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using libtcod;

using System.Data.SQLite;
using System.Runtime.Serialization.Formatters.Binary;
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
    /// This object holds the variables and functions for world generation.
    /// </summary>
    public class WorldMap
    {
        #region "Map constants"

        public int CELL_WIDTH = 200;
        public int CELL_HEIGHT = 200;
        public int CELL_DEPTH = 5;

        public int CELLS_X = 10;
        public int CELLS_Y = 10;
        public int CELLS_Z = 20;

        public int GLOBAL_WIDTH;
        public int GLOBAL_HEIGHT;
        public int GLOBAL_DEPTH;

        public int GROUND_LEVEL = 45;

        public static ushort TILE_AIR = 0;
        public static ushort TILE_DIRT = 1;
        public static ushort TILE_STONE_WALL = 2;
        public static ushort TILE_GRAVEL = 3;
        public static ushort TILE_SAND = 4;
        public static ushort TILE_WATER = 5;
        public static ushort TILE_STAIR_UP = 6;
        public static ushort TILE_STAIR_DOWN = 7;
        public static ushort TILE_STAIR_UP_DOWN = 8;

        public static ushort TILE_STONE_FLOOR = 9;

        #endregion

        Cell[, ,] cells;

        Dictionary<ushort, Tile> tileDict;

        uint seed = 133337;
        TCODRandom rand;
        TCODNoise noise;

        SQLiteConnection dbconn;

        /// <summary>
        /// Initializes a new WorldMap object, which provides cell retrieval and terrain generation functions.
        /// </summary>
        /// <param name="seed">The map seed.</param>
        /// <param name="dbconn">The connection to the object database.</param>
        /// <param name="parameters">The parameters for terrain generation.</param>
        public WorldMap(uint seed, SQLiteConnection dbconn, int[] parameters)
        {
            //Initialization
            this.dbconn = dbconn;
            this.seed = seed;
            rand = new TCODRandom(seed, TCODRandomType.ComplementaryMultiplyWithCarry);
            noise = new TCODNoise(2, rand);

            CELL_WIDTH = parameters[0];
            CELL_HEIGHT = parameters[1];
            CELL_DEPTH = parameters[2];

            CELLS_X = parameters[3];
            CELLS_Y = parameters[4];
            CELLS_Z = parameters[5];

            GLOBAL_WIDTH = CELLS_X * CELL_WIDTH;
            GLOBAL_HEIGHT = CELLS_Y * CELL_HEIGHT;
            GLOBAL_DEPTH = CELLS_Z * CELL_DEPTH;

            GROUND_LEVEL = parameters[6];

            //Create Cells
            cells = new Cell[CELLS_X, CELLS_Y, CELLS_Z];
            int id = 0;
            for (int x = 0; x < CELLS_X; x++)
            {
                for (int y = 0; y < CELLS_Y; y++)
                {
                    for (int z = 0; z < CELLS_Z; z++)
                    {
                        cells[x, y, z] = new Cell(x * CELL_WIDTH, y * CELL_HEIGHT, z * CELL_DEPTH, id, this);
                        id++;
                    }
                }
            }

            //Create Tiles
            makeDefaultTileSetup();
            loadTileDict();
            makeTestDiffs();
        }

        /// <summary>
        /// This function creates the Tiles and writes them and their mappings (ushort ID's) to DB.
        /// </summary>
        private void makeDefaultTileSetup()
        {
            //Setup/Construct the default tiles
            Tile Air = new Tile("Air", "You should not be seeing this. Please contact your local FBI office.", ' ', false, false);
            Tile Dirt = new Tile("Dirt", "A patch of dirt with small gravel and traces of sand.", '.', true, true);
            Tile Gravel = new Tile("Gravel", "A patch of gravel with traces of sand and dirt.", '.', true, true);
            Tile Sand = new Tile("Sand", "A patch of sand.", '.', true, true);
            Tile Stone = new Tile("Stone Wall", "A wall of stones stacked on top of each other. It doesn't look very solid.", '#', true, true);
            Tile StoneF = new Tile("Stone Floor", "A floor of stones.", '.', true, true);
            Tile Water = new Tile("Water", "A lake.", '~', true, true);

            Tile StairUp = new Tile("Stairs", "These stairs lead up a level.", '^', false, true);
            Tile StairDown = new Tile("Stairs", "These stairs lead down a level.", 'v', false, true);
            Tile StairUpDown = new Tile("Stairs", "These stairs lead up or down a level.", '/', false, true);


            //Initalize the default tiles
            Air.Init(null, null);
            Dirt.Init(new libtcod.TCODColor(205, 133, 63), new libtcod.TCODColor(205, 133, 63));
            Gravel.Init(new libtcod.TCODColor(112, 128, 144), new libtcod.TCODColor(210, 180, 140));
            Sand.Init(new libtcod.TCODColor(238, 221, 130), new libtcod.TCODColor(238, 221, 130));
            Stone.Init(libtcod.TCODColor.grey, libtcod.TCODColor.darkerGrey);
            StoneF.Init(libtcod.TCODColor.darkerGrey, libtcod.TCODColor.grey);
            Water.Init(libtcod.TCODColor.blue, libtcod.TCODColor.darkBlue);

            StairUp.Init(libtcod.TCODColor.lightGrey, libtcod.TCODColor.grey);
            StairDown.Init(libtcod.TCODColor.lightGrey, libtcod.TCODColor.grey);
            StairUpDown.Init(libtcod.TCODColor.lightGrey, libtcod.TCODColor.grey);

            //Put all the default times
            Dictionary<ushort, Tile> tiles = new Dictionary<ushort, Tile>();
            tiles.Add(TILE_AIR, Air);
            tiles.Add(TILE_DIRT, Dirt);
            tiles.Add(TILE_GRAVEL, Gravel);
            tiles.Add(TILE_SAND, Sand);
            tiles.Add(TILE_STONE_WALL, Stone);

            tiles.Add(TILE_STONE_FLOOR, StoneF);
            tiles.Add(TILE_WATER, Water);
            tiles.Add(TILE_STAIR_UP, StairUp);
            tiles.Add(TILE_STAIR_DOWN, StairDown);
            tiles.Add(TILE_STAIR_UP_DOWN, StairUpDown);

            byte[] arr;
            string data;
            int fore, back;

            //Set up database connection and serialization objects
            MemoryStream fstream = new MemoryStream();
            BinaryFormatter serializer = new BinaryFormatter();
            SQLiteCommand command = new SQLiteCommand(dbconn);

            //Prepare DB by removing all existing tiles and resetting the tables
            command.CommandText = "DROP TABLE tiles";
            command.ExecuteNonQuery();
            command.CommandText = "DROP TABLE tile_mapping";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE tiles (guid BLOB NOT NULL PRIMARY KEY, data BLOB NOT NULL, fore BLOB NOT NULL, back BLOB NOT NULL);";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE tile_mapping (id BLOB NOT NULL PRIMARY KEY, guid BLOB NOT NULL);";
            command.ExecuteNonQuery();

            //Begin the SQLiteTransaction
            SQLiteTransaction tr = dbconn.BeginTransaction();
            command.Transaction = tr;

            //Iterate through all tiles
            foreach (KeyValuePair<ushort, Tile> kv in tiles)
            {
                Tile t = kv.Value;

                //Call the save function of the tile which "uninitializes" it.
                t.Save();

                //Reset stream
                fstream.SetLength(0);
                fstream.Seek(0, SeekOrigin.Begin);

                //Do the actual serialization into the MemoryStream
                serializer.Serialize(fstream, t);

                arr = new byte[fstream.Length];

                //Encode the item's ForeColor into a single integer
                //NOTE: The "technique" used is called bit shift.
                fore = 0;
                back = 0;
                if (t.ForeColor != null)
                    fore = t.ForeColor.Red << 16 | t.ForeColor.Green << 8 | t.ForeColor.Blue;
                if (t.BackColor != null)
                    back = t.BackColor.Red << 16 | t.BackColor.Green << 8 | t.BackColor.Blue;

                //Reset and read the MemoryStream into the byte array.
                fstream.Seek(0, SeekOrigin.Begin);
                fstream.Read(arr, 0, (int)fstream.Length);

                //Convert the hex-encoded byte array extracted from the serialized MemoryStream
                //into an ascii-encoded string (and remove all dashes).
                data = BitConverter.ToString(arr).Replace("-", string.Empty);

                //Prepare the actual SQL command 
                //NOTE: Please refer to SQL documentation for detailed information.
                command.CommandText = "INSERT INTO tiles (guid, data, fore, back) VALUES ('" + t.GUID + "', '" + data + "', " + fore + ", " + back + ")";
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO tile_mapping (id, guid) VALUES ('" + kv.Key + "', '" + t.GUID + "')";
                command.ExecuteNonQuery();
            }

            //Execute and commit SQLite command and transaction.
            tr.Commit();

            //Cleanup
            command.Dispose();
            tr.Dispose();
            fstream.Close();
        }

        /// <summary>
        /// This function loads the Tiles (tile definitions).
        /// </summary>
        private void loadTileDict()
        {
            //Initialize the tile dictionary
            tileDict = new Dictionary<ushort, Tile>();

            //Initialize the temporary dictionary (which holds the tile ID <-> tile GUID links)
            Dictionary<string, ushort> tempDict = new Dictionary<string, ushort>();

            //Setup vars
            string guid;
            byte[] data;
            int fore, back;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter deserializer = new BinaryFormatter();

            SQLiteCommand command = new SQLiteCommand(dbconn);
            SQLiteDataReader reader;

            //Prepare retrieval of the Tile Maps
            command.CommandText = "SELECT * FROM tile_mapping";
            reader = command.ExecuteReader();

            //DB Results have the following format:
            // reader[0] = ID
            // reader[1] = GUID

            while (reader.Read())
            {
                tempDict.Add(Util.ByteArrayToString((byte[])reader[1]), Convert.ToUInt16(Util.ByteArrayToString((byte[])reader[0])));
            }

            //Retrieve all tiles for which the GUID was mapped to a tile ID
            // (as loaded from tile_mapping into tempDict)
            String whereClause = "";
            string[] keys = tempDict.Keys.ToArray<string>();

            for (int i = 0; i < keys.Length; i++)
            {
                whereClause += "guid='"+ keys[i] +"'";
                if (i != keys.Length - 1)
                    whereClause += " or ";
            }

            reader.Dispose();
            reader.Close();

            SQLiteDataReader reader2;
            command.CommandText = "SELECT * FROM tiles WHERE " + whereClause;
            reader2 = command.ExecuteReader();

            //DB Results have the following format:
            // reader[0] = GUID
            // reader[1] = data
            // reader[2] = fore
            // reader[3] = back

            while (reader2.Read())
            {
                //Get the GUID
                guid = Util.ByteArrayToString((byte[])reader2[0]);

                //Parse this pesky string of ASCII encoded bytes into an actual proper hex-encoded byte array.
                data = Util.ConvertDBByteArray((byte[])reader2[1]);

                fore = Convert.ToInt32(Util.ByteArrayToString((byte[])reader2[2]));
                back = Convert.ToInt32(Util.ByteArrayToString((byte[])reader2[3]));

                fstream.SetLength(0);
                fstream.Seek(0, SeekOrigin.Begin);

                //Write all the data from the freshly parsed byte array into the MemoryStream
                fstream.Write(data, 0, data.Length);

                //Reset the MemoryStream
                fstream.Seek(0, SeekOrigin.Begin);

                //Extract (deserialize) the tile from the MemoryStream
                Tile t = (Tile)deserializer.Deserialize(fstream);

                //And initialize the new and shining tile
                TCODColor forecolor = null;
                TCODColor backcolor = null;

                if (fore != 0)
                    forecolor = new TCODColor(fore >> 16, fore >> 8 & 0xFF, fore & 0xFF);
                if (back != 0)
                    backcolor = new TCODColor(back >> 16, back >> 8 & 0xFF, back & 0xFF);
                t.Init(forecolor, backcolor);

                //Add the initialized tile to the tileDict
                tileDict.Add(tempDict[guid], t);
            }

            //Cleanup
            reader2.Close();
            reader2.Dispose();
            fstream.Close();

            //Done!
            return;
        }

        /// <summary>
        /// For testing purposes, this creates some custom changes to the map.
        /// </summary>
        private void makeTestDiffs()
        {
            //This function creates the "steps" leading out of the bunker.
            SQLiteCommand command = new SQLiteCommand(dbconn);
            SQLiteTransaction trans = dbconn.BeginTransaction();
            int y = 1300;

            for (int x = 1300; x < 1400; x++)
            {
                for (int z = 30; z < 100; z++)
                {
                    for (int st = 0; st < 30; st++)
                    {
                        if (x == 1325 + st && z >= 31 + st && z < 35 + st)
                        {
                            command.CommandText = "INSERT INTO diff_map (cell_id, abs_x, abs_y, abs_z, tile) VALUES ('" +
                                GetCellIDFromCoordinates(x, y, z) + "','" +
                                x + "','" + y + "','" + z + "','" +
                                TILE_AIR + "')";
                            command.ExecuteNonQuery();
                        }

                        if (x == 1325 + st && z == 30 + st)
                        {
                            command.CommandText = "INSERT INTO diff_map (cell_id, abs_x, abs_y, abs_z, tile) VALUES ('" +
                                GetCellIDFromCoordinates(x, y, z) + "','" +
                                x + "','" + y + "','" + z + "','" +
                                TILE_STAIR_UP_DOWN + "')";
                            command.ExecuteNonQuery();
                        }
                    }

                    if (x > 1318 && x < 1325 && z == 31)
                    {
                        command.CommandText = "INSERT INTO diff_map (cell_id, abs_x, abs_y, abs_z, tile) VALUES ('" +
                            GetCellIDFromCoordinates(x, y, z) + "','" +
                            x + "','" + y + "','" + z + "','" +
                            TILE_AIR + "')";
                        command.ExecuteNonQuery();
                    }
                    if (x > 1318 && x < 1325 && z == 30)
                    {
                        command.CommandText = "INSERT INTO diff_map (cell_id, abs_x, abs_y, abs_z, tile) VALUES ('" +
                            GetCellIDFromCoordinates(x, y, z) + "','" +
                            x + "','" + y + "','" + z + "','" +
                            TILE_GRAVEL + "')";
                        command.ExecuteNonQuery();
                    }
                }
            }

            //Cleanup
            trans.Commit();
            trans.Dispose();
            command.Dispose();
        }

        public Dictionary<ushort, bool> GetLOSBlockerTiles()
        {
            Dictionary<ushort, bool> temp = new Dictionary<ushort, bool>();

            foreach (KeyValuePair<ushort, Tile> kv in tileDict)
            {
                temp.Add(kv.Key, kv.Value.BlocksLOS);
            }
            return temp;
        }

        public Dictionary<ushort, bool> GetMoveBlockerTiles()
        {
            Dictionary<ushort, bool> temp = new Dictionary<ushort, bool>();

            foreach (KeyValuePair<ushort, Tile> kv in tileDict)
            {
                temp.Add(kv.Key, kv.Value.BlocksMovement);
            }
            return temp;
        }

        /// <summary>
        /// This function returns the Tile Object linked to the ushort ID.
        /// </summary>
        public Tile GetTileFromID(ushort id)
        {
            return tileDict[id];
        }

        /// <summary>
        /// This function returns the cell at the given coordinates.
        /// </summary>
        public Cell GetCellFromCoordinates(int x, int y, int z)
        {
            //Check if given coords are within world bounds
            if (x > GLOBAL_WIDTH || x < 0 ||
                y > GLOBAL_HEIGHT || y < 0 ||
                z > GLOBAL_DEPTH || z < 0)
                return null;

            int rx = (x / CELL_WIDTH); //TRUNCATED!
            int ry = (y / CELL_HEIGHT);
            int rz = (z / CELL_DEPTH);

            return cells[rx, ry, rz];
        }

        /// <summary>
        /// This function returns the ID of the cell at the given coordinates.
        /// </summary>
        public int GetCellIDFromCoordinates(int x, int y, int z)
        {
            //Check if given coords are within world bounds
            if (x > GLOBAL_WIDTH || x < 0 ||
                y > GLOBAL_HEIGHT || y < 0 ||
                z > GLOBAL_DEPTH || z < 0)
                return -1;

            int rx = (x / CELL_WIDTH); //TRUNCATED!
            int ry = (y / CELL_HEIGHT);
            int rz = (z / CELL_DEPTH);

            return cells[rx, ry, rz].CellID;
        }

        /// <summary>
        /// This function returns the cell at the given relative position of the given cell.
        /// </summary>
        public Cell GetAdjacentCell(int dx, int dy, int dz, Cell c)
        {
            int rx = (c.X / CELL_WIDTH); //TRUNCATED!
            int ry = (c.Y/ CELL_HEIGHT);
            int rz = (c.Z / CELL_DEPTH);

            if (rx + dx >= 0 && ry + dy >= 0 && rz + dz >= 0)
            {
                try
                {
                    return cells[rx + dx, ry + dy, rz + dz];
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// This function returns the tile ID at the given coordinates using the default heightmap and random seed.
        /// </summary>
        public ushort GenerateTerrain(int x, int y, int z)
        {
            float[] f = { (float)x / (float)GLOBAL_WIDTH * 1000, (float)y / (float)GLOBAL_HEIGHT * 1000 };
            return GenerateTerrain(x, y, z, GROUND_LEVEL, ((double)noise.getSimplexTurbulence(f, 1)));
        }

        /// <summary>
        /// This function returns the tile ID at the given coordinates.
        /// </summary>
        private ushort GenerateTerrain(int x, int y, int z, float hm_val, double rand)
        {
            
            if (y > 1260 && y < 1270)
            {
                if (x > 1260 && x < 1270)
                {
                    return TILE_STONE_WALL;
                }
            }
            

            if (x > 1280 && x < 1320 && z < 41)
            {
                if (y > 1280 && y < 1320)
                {
                    if ((y == 1281 || y == 1319) && (z > 30 && z < 41))
                        return TILE_STONE_WALL;
                    if ((x == 1281 || x == 1319) && (z > 30 && z < 41))
                        return TILE_STONE_WALL;
                    if (z < 10)
                        return TILE_GRAVEL;
                    if (z == 30 || z == 40)
                        return TILE_STONE_FLOOR;

                    if (z == 31 && (rand < 0.05))
                        return TILE_STONE_WALL;
                    return TILE_AIR;
                }
            }

            if (z < hm_val)
            {
                if (z < 10)
                    return TILE_WATER;
                if (z < 11)
                    return TILE_SAND;
                if (rand < 0.1)
                    return TILE_SAND;
                if (rand < 0.15)
                    return TILE_GRAVEL;
                return TILE_DIRT;
            }

            return TILE_AIR;
        }
    }
}
