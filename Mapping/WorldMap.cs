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
    public class WorldMap
    {
        public int Width, Height, Depth;
        public int CellsX, CellsY, CellsZ;

        public static int CELL_WIDTH = 300;
        public static int CELL_HEIGHT = 300;
        public static int CELL_DEPTH = 30;

        public static int CELLS_X = 10;
        public static int CELLS_Y = 10;
        public static int CELLS_Z = 6;

        public static int GLOBAL_WIDTH = CELLS_X * CELL_WIDTH;
        public static int GLOBAL_HEIGHT = CELLS_Y * CELL_HEIGHT;
        public static int GLOBAL_DEPTH = CELLS_Z * CELL_DEPTH;

        public static float HEIGHTMAP_SCALER = 1.0f;
        public static int HEIGHTMAP_NORMALIZER_LOW = 0;
        public static int HEIGHTMAP_NORMALIZER_HIGH = GLOBAL_DEPTH/2-3;

        public static ushort TILE_AIR = 0;
        public static ushort TILE_DIRT = 1;
        public static ushort TILE_STONE_WALL = 2;
        public static ushort TILE_GRAVEL = 3;
        public static ushort TILE_SAND = 4;
        public static ushort TILE_WATER = 5;

        Cell[, ,] cells;

        Dictionary<ushort, Tile> tileDict;

        TCODRandom rand;
        TCODNoise noise;

        public String MapFile;
        SQLiteConnection dbconn;

        public WorldMap(String mapFile, SQLiteConnection dbconn)
        {
            MapFile = mapFile;
            this.dbconn = dbconn;
            makeTestSetup();
        }

        private void makeTestSetup()
        {
            Width = GLOBAL_WIDTH;
            Height = GLOBAL_HEIGHT;
            Depth = GLOBAL_DEPTH;
            CellsX = WorldMap.CELLS_X;
            CellsY = WorldMap.CELLS_Y;
            CellsZ = WorldMap.CELLS_Z;

            uint hm_seed = 133337;

            //Create Cells
            cells = new Cell[CellsX, CellsY, CellsZ];
            int id = 0;
            for (int x = 0; x < CellsX; x++)
            {
                for (int y = 0; y < CellsY; y++)
                {
                    
                    //cells[x, y, 0] = new Cell(x * CELL_WIDTH, y * CELL_HEIGHT, 0, id, this);
                    //id++;
                    for (int z = 0; z < CellsZ; z++)
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
            
            //COLOR EACH CELL RANDOMLY BY ASSIGNING A SPECIAL TILE TO EACH CELL
            /*
            Random ran = new Random();
            for (int i = 0; i < CellsX*CellsY; i++)
            {
                libtcod.TCODColor col = new TCODColor(ran.Next(0, 255), ran.Next(0, 255), ran.Next(0, 255));

                Tile Temp = new Tile("CELL " + i, "DEBUG", TCODColor.silver, col, ' ', false, false); //i.ToString().ToCharArray()[0], false, false);
                tileDict.Add((byte)(i + 5), Temp);
            }
             * 

            Random ran = new Random();
            for (int i = 0; i < CellsX * CellsY * CellsZ; i++)
            {
                libtcod.TCODColor col = new TCODColor(ran.Next(0, 255), ran.Next(0, 255), ran.Next(0, 255));

                Tile Temp = new Tile("CELL " + i, "DEBUG", TCODColor.silver, col, ' ', true, false); //i.ToString().ToCharArray()[0], false, false);
                //Tile Temp = new Tile("CELL " + i, "DEBUG",  col, null, ',', true, false); //i.ToString().ToCharArray()[0], false, false);
                
                tileDict.Add((byte)(i + 5), Temp);
            }
            *
             */
             
            //Create Data (if not existant)
            
            //Heightmap
            /*
            Stopwatch hmsw = new Stopwatch();
            Console.Write("Generating heightmap with seed " + hm_seed + "...");
            hmsw.Start();

            TCODHeightMap map = makeHeightMap(WorldMap.GLOBAL_WIDTH, WorldMap.GLOBAL_HEIGHT, hm_seed);

            hmsw.Stop();
            Console.WriteLine("done!");
            Console.WriteLine("Heightmap generation took " + hmsw.ElapsedMilliseconds + "ms.");

            Console.Write("Exporting heightmap...");
            hmsw.Start();

            Util.ExportHeightmapAsBitmap(map, "test.bmp");

            Console.WriteLine("done!");
            Console.WriteLine("Heightmap export took " + hmsw.ElapsedMilliseconds + "ms.");

            //map.normalize(HEIGHTMAP_NORMALIZER_LOW, HEIGHTMAP_NORMALIZER_HIGH);
            */  
            //All other stuff
            
            rand = new TCODRandom(hm_seed, TCODRandomType.ComplementaryMultiplyWithCarry);
            noise = new TCODNoise(2, rand);

            /*
            FileStream fstream = new FileStream(MapFile, FileMode.Create);
            byte[] temp = new byte[Height * Depth];


            float[] f = new float[2];

            Stopwatch total = new Stopwatch();
            Console.Write("Starting map data generation");
            total.Start();

            for (int x = 0; x < Width; x++)
            {
                temp = new byte[Height * Depth];
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Depth; z++)
                    {
                        f[0] = (float)x / (float)WorldMap.GLOBAL_WIDTH * 10000;
                        f[1] = (float)y / (float)WorldMap.GLOBAL_HEIGHT * 10000;

                        //f[2] = (float)z / (float)WorldMap.GLOBAL_DEPTH * 10;
                        //temp[(y * Depth) + z] = GenerateTerrain(x, y, z, map.getValue(x, y), (((double)noise.getSimplexTurbulence(f, 1))));

                        temp[(y * Depth) + z] = GenerateTerrain(x, y, z, getHeightMapValue(x,y, noise), (((double)noise.getSimplexTurbulence(f, 1))));
                        if (x == 0 && y == 0)
                            Debug.WriteLine("z: " + z + " -  " + temp[(y * Depth) + z]);
                    }
                }
                fstream.Write(temp, 0, (Height * Depth));
                Console.Write(".");
            }
            Console.WriteLine("done.");

            Console.WriteLine("Map data generation complete. All queries took " + total.ElapsedMilliseconds + "ms.");
            fstream.Close();
            fstream.Close();
            GC.Collect();*/
            
        }

        private void makeDefaultTileSetup()
        {
            Tile Air = new Tile("Air", "You should not be seeing this. Please contact your local FBI office.", ' ', false, false);
            Tile Dirt = new Tile("Dirt", "A patch of dirt with small gravel and traces of sand.", '.', true, false);
            Tile Gravel = new Tile("Gravel", "A patch of gravel with traces of sand and dirt.", '.', true, false);
            Tile Sand = new Tile("Sand", "A patch of sand.", '.', true, false);
            Tile Stone = new Tile("Stone Wall", "A wall of stones stacked on top of each other. It doesn't look very solid.", '#', true, true);
            Tile Water = new Tile("Water", "A lake.", '~', true, false);

            Air.Init(null, null);
            Dirt.Init(new libtcod.TCODColor(205, 133, 63), new libtcod.TCODColor(205, 133, 63));
            Gravel.Init(new libtcod.TCODColor(112, 128, 144), new libtcod.TCODColor(210, 180, 140));
            Sand.Init(new libtcod.TCODColor(238, 221, 130), new libtcod.TCODColor(238, 221, 130));
            Stone.Init(libtcod.TCODColor.grey, libtcod.TCODColor.darkerGrey);
            Water.Init(libtcod.TCODColor.blue, libtcod.TCODColor.darkBlue);

            Dictionary<ushort, Tile> tiles = new Dictionary<ushort, Tile>();
            tiles.Add(TILE_AIR, Air);
            tiles.Add(TILE_DIRT, Dirt);
            tiles.Add(TILE_GRAVEL, Gravel);
            tiles.Add(TILE_SAND, Sand);
            tiles.Add(TILE_STONE_WALL, Stone);
            tiles.Add(TILE_WATER, Water);

            byte[] arr;
            string data;
            int fore, back;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter serializer = new BinaryFormatter();
            SQLiteCommand command = new SQLiteCommand(dbconn);

            command.CommandText = "DROP TABLE tiles";
            command.ExecuteNonQuery();
            command.CommandText = "DROP TABLE tile_mapping";
            command.ExecuteNonQuery();

            command.CommandText = "CREATE TABLE tiles (guid BLOB NOT NULL PRIMARY KEY, data BLOB NOT NULL, fore BLOB NOT NULL, back BLOB NOT NULL);";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE tile_mapping (id BLOB NOT NULL PRIMARY KEY, guid BLOB NOT NULL);";
            command.ExecuteNonQuery();

            //Begin a new SQLiteTransaction 
            //NOTE: A SQL Transaction stores several SQL commands
            //before commiting them to the database. This can save a
            //lot of time.
            SQLiteTransaction tr = dbconn.BeginTransaction();
            command.Transaction = tr;

            foreach (KeyValuePair<ushort, Tile> kv in tiles)
            {
                Tile t = kv.Value;

                //Call the save function of the item/object which "uninitializes" it.
                t.Save();

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

        private void loadTileDict()
        {
            tileDict = new Dictionary<ushort, Tile>();

            Dictionary<string, ushort> tempDict = new Dictionary<string, ushort>();

            string guid;
            byte[] data;
            int fore, back;

            MemoryStream fstream = new MemoryStream();
            BinaryFormatter deserializer = new BinaryFormatter();

            SQLiteCommand command = new SQLiteCommand(dbconn);
            SQLiteDataReader reader;

            command.CommandText = "SELECT * FROM tile_mapping";
            reader = command.ExecuteReader();

            //DB Results have the following format:
            // reader[0] = ID
            // reader[1] = GUID

            while (reader.Read())
            {
                tempDict.Add(Util.ByteArrayToString((byte[])reader[1]), Convert.ToUInt16(Util.ByteArrayToString((byte[])reader[0])));
            }

            //TODO: Get relevant tiles, deserialize
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

        private void makeTestDiffs()
        {
            SQLiteCommand command = new SQLiteCommand(dbconn);
            SQLiteTransaction trans = dbconn.BeginTransaction();
            int y = 1300;

            for (int x = 1300; x < 1400; x++)
            {
                for (int z = 30; z < 100; z++)
                {
                    for (int st = 0; st < 50; st++)
                    {
                        if (x == 1325 + st && z >= 32 + st && z < 35 + st)
                        {
                            command.CommandText = "INSERT INTO diff_map (cell_id, abs_x, abs_y, abs_z, tile) VALUES ('" +
                                GetCellIDFromCoordinates(x, y, z) + "','" +
                                x + "','" + y + "','" + z + "','" +
                                TILE_AIR + "')";
                            command.ExecuteNonQuery();
                        }

                        if (x == 1325 + st && z == 31 + st)
                        {
                            command.CommandText = "INSERT INTO diff_map (cell_id, abs_x, abs_y, abs_z, tile) VALUES ('" +
                                GetCellIDFromCoordinates(x, y, z) + "','" +
                                x + "','" + y + "','" + z + "','" +
                                TILE_GRAVEL + "')";
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

            trans.Commit();
            trans.Dispose();
            command.Dispose();
        }
        
        private TCODHeightMap makeHeightMap(int width, int height, uint seed)
        {
            TCODHeightMap map = new TCODHeightMap(width, height);
            TCODRandom rand = new TCODRandom(seed,TCODRandomType.ComplementaryMultiplyWithCarry);
            TCODNoise noise = new TCODNoise(2, rand); //Provides 2D-noisegen with default RNG (Complementary-Multiply-With-Carry)

            float[] f = new float[2];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    f[0] = (float)x / (float)WorldMap.GLOBAL_WIDTH * WorldMap.HEIGHTMAP_SCALER;
                    f[1] = (float)y / (float)WorldMap.GLOBAL_HEIGHT * WorldMap.HEIGHTMAP_SCALER;

                    map.setValue(x, y, noise.getSimplexNoise(f));
                    //map.setValue(x, y, getHeightMapValue(x, y, noise));
                }
            }

            return map;
        }

        private float getHeightMapValue(int x, int y, TCODNoise noise)
        {
            float[] f = { (float)x / (float)WorldMap.GLOBAL_WIDTH * (float)WorldMap.HEIGHTMAP_SCALER, (float)y / (float)WorldMap.GLOBAL_HEIGHT * (float)WorldMap.HEIGHTMAP_SCALER };

            float z = noise.getSimplexNoise(f);
            
            return (((float)WorldMap.HEIGHTMAP_NORMALIZER_HIGH - (float)WorldMap.HEIGHTMAP_NORMALIZER_LOW) * ((z+1.0f)/2.0f)) + (float)WorldMap.HEIGHTMAP_NORMALIZER_LOW;
        }

        public void CompressMapFile()
        {
            FileStream inStream = new FileStream(MapFile, FileMode.Open);
            FileStream outStream = new FileStream(MapFile + ".gz", FileMode.Create);
            GZipStream compressor = new GZipStream(outStream, CompressionMode.Compress);

            inStream.CopyTo(compressor);

            compressor.Close();
            outStream.Close();
            inStream.Close();
        }

        public void DecompressMapFile()
        {
            FileStream inStream = new FileStream(MapFile + ".gz", FileMode.Open);
            FileStream outStream = new FileStream(MapFile, FileMode.Create);
            GZipStream decompressor = new GZipStream(inStream, CompressionMode.Decompress);

            decompressor.CopyTo(outStream);

            decompressor.Close();
            outStream.Close();
            inStream.Close();
        }

        public Tile GetTileFromID(ushort id)
        {
            return tileDict[id];
        }

        public Cell GetCellFromCoordinates(int x, int y, int z)
        {
            int rx = (x / CELL_WIDTH); //TRUNCATED!
            int ry = (y / CELL_HEIGHT);
            int rz = (z / CELL_DEPTH);

            return cells[rx, ry, rz];
        }

        /// <summary>
        /// This function returns the cell at the given relative position of the given cell.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dz"></param>
        /// <param name="c"></param>
        /// <returns></returns>
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

        public Cell GetAdjacentCell(int dx, int dy, int dz, Cell c, System.Threading.Thread t)
        {
            int rx = (c.X / CELL_WIDTH); //TRUNCATED!
            int ry = (c.Y / CELL_HEIGHT);
            int rz = (c.Z / CELL_DEPTH);

            if (rx + dx >= 0 && ry + dy >= 0 && rz + dz >= 0)
            {
                try
                {
                    t.Interrupt();
                    return cells[rx + dx, ry + dy, rz + dz];
                }
                catch
                {
                    t.Interrupt();
                    return null;
                }
            }

            t.Interrupt();
            return null;
        }

        public int GetCellIDFromCoordinates(int x, int y, int z)
        {
            //TODO: FALSE COORDINATES HANDLING !!!

            int rx = (x / CELL_WIDTH); //TRUNCATED!
            int ry = (y / CELL_HEIGHT);
            int rz = (z / CELL_DEPTH);

            return cells[rx, ry, rz].CellID;
        }

        
        public ushort GenerateTerrain(int x, int y, int z)
        {
            float[] f = { (float)x / (float)WorldMap.GLOBAL_WIDTH * 1000, (float)y / (float)WorldMap.GLOBAL_HEIGHT * 1000 };
            return GenerateTerrain(x, y, z, getHeightMapValue(x, y, noise), ((double)noise.getSimplexTurbulence(f, 1)));
        }
        

        public ushort GenerateTerrain(int x, int y, int z, float hm_val, double rand)
        {
            /*
            if (y == 1300)
            {
                for (int st = 0; st < 50; st++)
                {
                    if (x == 1325 + st && z >= 32 + st && z < 35 + st)
                        return TILE_AIR;
                    if (x == 1325 + st && z == 31 + st)
                        return TILE_GRAVEL;
                }

                if (x > 1318 && x < 1325 && z == 31)
                    return TILE_AIR;
                if (x > 1318 && x < 1325 && z == 30)
                    return TILE_GRAVEL;

            }
            */

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
                        return TILE_STONE_WALL;

                    return TILE_AIR;
                }
            }

            if (z < hm_val)
            {
                //return (byte)(GetCellIDFromCoordinates(x, y, z) + 5);
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
