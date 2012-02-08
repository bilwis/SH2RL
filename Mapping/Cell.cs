using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

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
    /// This object represents a certain part of a map. They can be loaded and saved seperately.
    /// </summary>
    public class Cell
    {
        byte[, ,] tileMap;  //This is the three-dimensional byte array holding the ID of the tile
                            //at that location.

        public int X, Y, Z; //These the absolute coordinates of the upper left corner of the cell.
        public int CellID;  //This is the ID of the Cell in the WorldMap/Map context

        WorldMap world;     //This is a reference to the WorldMap this cell is a part of

        /// <summary>
        /// Creates a new cell.
        /// </summary>
        /// <param name="X">The X coordinate of the upper left corner of the cell.</param>
        /// <param name="Y">The Y coordinate of the upper left corner of the cell.</param>
        /// <param name="Z">The Z coordinate of the upper left corner of the cell.</param>
        /// <param name="cellID">The (unique) cell id in the WorldMap context.</param>
        /// <param name="world">The WorldMap this cell is a part of.</param>
        public Cell(int X, int Y, int Z, int cellID, WorldMap world)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.world = world;
            this.CellID = cellID;
        }

        /// <summary>
        /// Returns the tile at the given location, or null if location is not inside this cell.
        /// </summary>
        /// <returns>The Tile object holding the tile's properties, or null.</returns>
        public Tile GetTile(int abs_x, int abs_y, int abs_z)
        {
            try
            {
                Tile t = world.GetTileFromID(tileMap[abs_x - X, abs_y - Y, abs_z - Z]);
                return t;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the ID of the tile at the given location.
        /// </summary>
        /// <returns>A byte representing a tile ID.</returns>
        public byte GetTileID(int abs_x, int abs_y, int abs_z)
        {
            if (abs_x < X || abs_x > X + WorldMap.CELL_WIDTH ||
                abs_y < Y || abs_y > Y + WorldMap.CELL_HEIGHT ||
                abs_z < Z || abs_z > Z + WorldMap.CELL_DEPTH)
                throw new Exception("Error while trying to retrieve Tile ID from Cell: The given coordinates are not within the called cell object.");

            return tileMap[abs_x - X, abs_y - Y, abs_z - Z];
        }

        /*
        /// <summary>
        /// Loads the tile ID's for every tile within the cell from the map file specified in the associated WorldMap object.
        /// </summary>
        public void Load()
        {
            //The map file contains the tile data in the following format: Every tile is represented
            //by a single byte. The map file consists of a continuous stream of bytes.
            //The first (GLOBAL_WIDTH*GLOBAL_DEPTH) bytes contain all data for X=0.
            //Of those bytes, the first GLOBAL_DEPTH bytes contain all data for X=0, Y=0.
            //Of those bytes, the first byte contains the data for X=0, Y=0, Z=0.

            //TODO: Implement a header for the map file.

            FileStream fstream = new FileStream(world.MapFile, FileMode.Open);

            tileMap = new byte[WorldMap.CELL_WIDTH, WorldMap.CELL_HEIGHT, WorldMap.CELL_DEPTH];

            byte[] temparr = new byte[WorldMap.CELL_HEIGHT*WorldMap.GLOBAL_DEPTH];

            int offset = 0;

            for (int x = 0; x < WorldMap.CELL_WIDTH; x++)
            {
                offset = (X+x)*(WorldMap.GLOBAL_HEIGHT * WorldMap.GLOBAL_DEPTH) + Y * WorldMap.GLOBAL_DEPTH;
                fstream.Seek(offset, SeekOrigin.Begin);
                fstream.Read(temparr, 0, WorldMap.CELL_HEIGHT * WorldMap.GLOBAL_DEPTH);

                for (int y = 0; y < WorldMap.CELL_HEIGHT; y++)
                {
                    for (int z = 0; z < WorldMap.CELL_DEPTH; z++)
                    {
                        tileMap[x, y, z] = temparr[y * WorldMap.GLOBAL_DEPTH + (Z + z)];
                    }
                }
            }

            fstream.Close();
        }
        */

        
        public bool Load()
        {
            tileMap = new byte[WorldMap.CELL_WIDTH, WorldMap.CELL_HEIGHT, WorldMap.CELL_DEPTH];

            for (int x = 0; x < WorldMap.CELL_WIDTH; x++)
            {
                for (int y = 0; y < WorldMap.CELL_HEIGHT; y++)
                {
                    for (int z = 0; z < WorldMap.CELL_DEPTH; z++)
                    {
                        tileMap[x, y, z] = world.GenerateTerrain(X + x, Y + y, Z + z);
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Unloads the cells tile array and calls the garbage collector.
        /// </summary>
        public void Unload()
        {
            tileMap = new byte[0,0,0];
            GC.Collect();
        }
    }
}
