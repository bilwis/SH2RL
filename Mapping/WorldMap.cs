using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using libtcod;

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

        public static byte TILE_AIR = 0;
        public static byte TILE_DIRT = 1;
        public static byte TILE_STONE_WALL = 2;
        public static byte TILE_GRAVEL = 3;
        public static byte TILE_SAND = 4;
        public static byte TILE_WATER = 5;

        Cell[, ,] cells;

        Dictionary<byte, Tile> tileDict;

        TCODRandom rand;
        TCODNoise noise;

        public String MapFile;

        public WorldMap(String mapFile)
        {
            MapFile = mapFile;
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
            tileDict = new Dictionary<byte,Tile>();
            Tile Air = new Tile("Air", "You should not be seeing this. Please contact your local FBI office.", null, null, ' ', false, false);
            Tile Dirt = new Tile("Dirt", "A patch of dirt with small gravel and traces of sand.", new libtcod.TCODColor(205, 133, 63), new libtcod.TCODColor(205, 133, 63), '.', true, false);
            Tile Gravel = new Tile("Gravel", "A patch of gravel with traces of sand and dirt.", new libtcod.TCODColor(112, 128, 144), new libtcod.TCODColor(210, 180, 140), '.', true, false);
            Tile Sand = new Tile("Sand", "A patch of sand.", new libtcod.TCODColor(238, 221, 130), new libtcod.TCODColor(238, 221, 130), '.', true, false);
            Tile Stone = new Tile("Stone Wall", "A wall of stones stacked on top of each other. It doesn't look very solid.", libtcod.TCODColor.grey, libtcod.TCODColor.darkerGrey, '#', true, true);
            Tile Water = new Tile("Water", "A lake.", libtcod.TCODColor.blue, libtcod.TCODColor.darkBlue, '~', true, false);
            tileDict.Add(TILE_AIR, Air);
            tileDict.Add(TILE_DIRT, Dirt);
            tileDict.Add(TILE_STONE_WALL, Stone);
            tileDict.Add(TILE_SAND, Sand);
            tileDict.Add(TILE_GRAVEL, Gravel);
            tileDict.Add(TILE_WATER, Water);
            
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

        public Tile GetTileFromID(byte id)
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

        
        public byte GenerateTerrain(int x, int y, int z)
        {
            float[] f = { (float)x / (float)WorldMap.GLOBAL_WIDTH * 10000, (float)y / (float)WorldMap.GLOBAL_HEIGHT * 10000 };
            return GenerateTerrain(x, y, z, getHeightMapValue(x, y, noise), ((double)noise.getSimplexTurbulence(f, 1)));
        }
        

        public byte GenerateTerrain(int x, int y, int z, float hm_val, double rand)
        {
            if (y == 1300)
            {
                for (int st = 0; st < 50; st++)
                {
                    if (x == 1325 + st  && z >= 32 + st && z < 35 + st)
                        return TILE_AIR;
                    if (x == 1325 + st  && z == 31 + st)
                        return TILE_GRAVEL;
                }
            
            if (x > 1318 && x < 1325  && z == 31)
                return TILE_AIR;
            if (x > 1318 && x < 1325  && z == 30)
                return TILE_GRAVEL;

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
