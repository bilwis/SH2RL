using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using libtcod;

namespace ShootyShootyRL.Mapping
{
    public class WorldMap
    {
        public int Width, Height, Depth;
        public int CellsX, CellsY, CellsZ;

        public static int CELL_WIDTH = 150;
        public static int CELL_HEIGHT = 150;
        public static int CELL_DEPTH = 10;

        public static int CELLS_X = 6;
        public static int CELLS_Y = 6;
        public static int CELLS_Z = 6;

        public static int GLOBAL_WIDTH = CELLS_X * CELL_WIDTH;
        public static int GLOBAL_HEIGHT = CELLS_Y * CELL_HEIGHT;
        public static int GLOBAL_DEPTH = CELLS_Z * CELL_DEPTH;

        public static float HEIGHTMAP_SCALER = 1.0f;

        public static byte TILE_AIR = 0;
        public static byte TILE_DIRT = 1;
        public static byte TILE_STONE_WALL = 2;
        public static byte TILE_GRAVEL = 3;
        public static byte TILE_SAND = 4;
        public static byte TILE_WATER = 5;

        Cell[, ,] cells;

        Dictionary<byte, Tile> tileDict;

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

            uint hm_seed = 133336;

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

            map.normalize(7, CELLS_Z * CELL_DEPTH / 4);
              
            //All other stuff
            
            FileStream fstream = new FileStream(MapFile, FileMode.Create);
            byte[] temp = new byte[Height * Depth];

            Random rand = new Random();

            //Stopwatch stw = new Stopwatch();
            Stopwatch total = new Stopwatch();
            Console.Write("Starting map data generation");
            //stw.Start();
            total.Start();

            for (int x = 0; x < Width; x++)
            {
                temp = new byte[Height * Depth];
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Depth; z++)
                    {
                        temp[(y * Depth)+z] = generateTerrain(x, y, z, map.getValue(x,y), rand.NextDouble());
                        if (x == 0 && y == 0)
                            Debug.WriteLine("z: " + z + " -  " + temp[(y * Depth) + z]);
                    }
                }
                fstream.Write(temp, 0, (Height * Depth));
                Console.Write(".");
                //stw.Restart();
            }
            Console.WriteLine("done.");

            Console.WriteLine("Map data generation complete. All queries took " + total.ElapsedMilliseconds + "ms.");
            fstream.Close();
            GC.Collect();
            
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

                    map.setValue(x, y, noise.getPerlinNoise(f));
                }
            }

            return map;
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

        public int GetCellIDFromCoordinates(int x, int y, int z)
        {
            //TODO: FALSE COORDINATES HANDLING !!!

            int rx = (x / CELL_WIDTH); //TRUNCATED!
            int ry = (y / CELL_HEIGHT);
            int rz = (z / CELL_DEPTH);

            return cells[rx, ry, rz].CellID;
        }

        private byte generateTerrain(int x, int y, int z, float hm_val, double rand)
        {
            if (x > 280 && x < 320)
            {
                if (y > 280 && y < 320)
                {
                    
                    if (x == 300 && y == 281 && (z == 11 || z == 11))
                        return TILE_AIR;
                    if (z == 10)
                        return TILE_GRAVEL;
                    if (z == 15)
                        return TILE_STONE_WALL;
                    if (y == 281 || y == 319)
                        return TILE_STONE_WALL;
                    if (x == 281 || x == 319)
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
                if (rand < 0.03)
                    return TILE_SAND;
                if (rand < 0.05)
                    return TILE_GRAVEL;
                return TILE_DIRT;
                 
            }

            return TILE_AIR;
        }
    }
}
