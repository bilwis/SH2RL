using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Text;
using System.Diagnostics;

using libtcod;
using ShootyShootyRL.Mapping;

namespace ShootyShootyRL
{
    public static class Util
    {
        public static int SLEEP_COLOR_DIM = 25;

        public enum DBLookupType
        {
            AICreature = 0,
            Item = 1,
            AI = 2,
            Faction = 3
        }

        public static double CalculateDistance(Object a, Object b)
        {
            return CalculateDistance(a.X, a.Y, a.Z, b.X, b.Y, b.Z);
        }

        public static double CalculateDistance(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            return Math.Sqrt(Math.Pow((double)x1 - (double)x2, 2.0d) + Math.Pow((double)y1 - (double)y2, 2.0d) + Math.Pow((double)z1 - (double)z2, 2.0d));
        }

        public static void ExportHeightmapAsBitmap(TCODHeightMap map, string filename)
        {
            TCODHeightMap m2 = new TCODHeightMap(WorldMap.GLOBAL_WIDTH, WorldMap.GLOBAL_HEIGHT);
            m2.copy(map);
            m2.add(1.0f);
            m2.scale(128);

            Debug.WriteLine("EXPORT A: "  + map.getValue(10, 10));
            Debug.WriteLine("EXPORT B: " + m2.getValue(10, 10));

            Bitmap bmp = new Bitmap(WorldMap.GLOBAL_WIDTH, WorldMap.GLOBAL_HEIGHT);
            int val = 0;
            int argb = 0xFF;


            for (int x = 0; x < WorldMap.GLOBAL_WIDTH; x++)
            {
                for (int y = 0; y < WorldMap.GLOBAL_HEIGHT; y++)
                {
                    val = (int) m2.getValue(x, y);
                    
                    bmp.SetPixel(x, y, Color.FromArgb((0xFF<<24)+(val<<16)+(val<<8)+val));
                }
            }
            bmp.Save(filename,System.Drawing.Imaging.ImageFormat.Bmp);
        }

        public static byte[] StringToByteArray(string str)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetBytes(str);
        }

        public static string ByteArrayToString(byte[] arr)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetString(arr);
        }
    }
}
