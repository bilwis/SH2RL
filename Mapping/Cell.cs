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
        ushort[, ,] tileMap;  //This is the three-dimensional byte array holding the ID of the tile
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
        public ushort GetTileID(int abs_x, int abs_y, int abs_z)
        {
            if (abs_x < X || abs_x > X + world.CELL_WIDTH ||
                abs_y < Y || abs_y > Y + world.CELL_HEIGHT ||
                abs_z < Z || abs_z > Z + world.CELL_DEPTH)
                throw new Exception("Error while trying to retrieve Tile ID from Cell: The given coordinates are not within the called cell object.");

            return tileMap[abs_x - X, abs_y - Y, abs_z - Z];
        }

        /// <summary>
        /// This function sets the tilemap at the given coordinates to the given tile id.
        /// </summary>
        public void SetTile(int abs_x, int abs_y, int abs_z, ushort tile)
        {
            if (abs_x < X || abs_x > X + world.CELL_WIDTH ||
                abs_y < Y || abs_y > Y + world.CELL_HEIGHT ||
                abs_z < Z || abs_z > Z + world.CELL_DEPTH)
                throw new Exception("Error while trying to set Tile at " + abs_x + ", " + abs_y + ", " + abs_z + ". Not in called Cell.");

            tileMap[abs_x - X, abs_y - Y, abs_z - Z] = tile;
        }
        
        /// <summary>
        /// This function generates the tilemap for this cell.
        /// </summary>
        public bool Load()
        {
            tileMap = new ushort[world.CELL_WIDTH, world.CELL_HEIGHT, world.CELL_DEPTH];

            for (int x = 0; x < world.CELL_WIDTH; x++)
            {
                for (int y = 0; y < world.CELL_HEIGHT; y++)
                {
                    for (int z = 0; z < world.CELL_DEPTH; z++)
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
            tileMap = new ushort[0,0,0];
            GC.Collect();
        }
    }
}
