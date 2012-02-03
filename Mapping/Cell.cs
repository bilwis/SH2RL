using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ShootyShootyRL.Mapping
{
    public class Cell
    {

        byte[, ,] tileMap;

        public int X, Y, Z; //These the absolute coordinates of the upper left corner of the cell
        public int CellID;
        WorldMap world;

        public Cell(int X, int Y, int Z, int cellID, WorldMap world)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.world = world;
            this.CellID = cellID;
        }

        public Tile GetTile(int abs_x, int abs_y, int abs_z)
        {
            return world.GetTileFromID(tileMap[abs_x - X, abs_y - Y, abs_z - Z]);
        }

        public byte GetTileID(int abs_x, int abs_y, int abs_z)
        {
            return tileMap[abs_x - X, abs_y - Y, abs_z - Z];
        }

        
        public void Load()
        {
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

        /*
        public void Load()
        {
            
            //TODO: Reorganize! Right now: entire file is loaded into memory every time, but one could improve by only loading the "relevant" X "columns" in data,
            // and perhaps even only the important Y's (much more mathemagic needed, though)

            FileStream fstream = new FileStream(world.MapFile,FileMode.Open);
            tileMap = new byte[WorldMap.CELL_WIDTH, WorldMap.CELL_HEIGHT, WorldMap.CELL_DEPTH];

            //fstream.Seek(((X) * (WorldMap.GLOBAL_HEIGHT * WorldMap.GLOBAL_DEPTH)), SeekOrigin.Begin); //Jump to relevant data

            byte[] temparr = new byte[WorldMap.GLOBAL_WIDTH * WorldMap.GLOBAL_HEIGHT * WorldMap.GLOBAL_DEPTH];
            fstream.Read(temparr, 0, WorldMap.GLOBAL_WIDTH * WorldMap.GLOBAL_HEIGHT * WorldMap.GLOBAL_DEPTH);
            fstream.Close();

            for (int x = 0; x < WorldMap.CELL_WIDTH; x++)
            {
                for (int y = 0; y < WorldMap.CELL_HEIGHT; y++)
                {
                    for (int z = 0; z < WorldMap.CELL_DEPTH; z++)
                    {
                        tileMap[x, y, z] = temparr[((X + x) * (WorldMap.GLOBAL_HEIGHT * WorldMap.GLOBAL_DEPTH)) + (Y + y) * WorldMap.GLOBAL_DEPTH + (Z + z)];
                    }
                }
            }
             
        }*/

        public void Unload()
        {
            //TODO: Handle Saving!
            tileMap = new byte[0,0,0];

            GC.Collect();
            
        }
    }
}
