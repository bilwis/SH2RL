using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Text;
using System.Diagnostics;

using libtcod;
using ShootyShootyRL.Mapping;

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

        public static void ExportHeightmapAsBitmap(WorldMap wm, TCODHeightMap map, string filename)
        {
            TCODHeightMap m2 = new TCODHeightMap(wm.GLOBAL_WIDTH, wm.GLOBAL_HEIGHT);
            m2.copy(map);
            m2.add(1.0f);
            m2.scale(128);

            Debug.WriteLine("EXPORT A: "  + map.getValue(10, 10));
            Debug.WriteLine("EXPORT B: " + m2.getValue(10, 10));

            Bitmap bmp = new Bitmap(wm.GLOBAL_WIDTH, wm.GLOBAL_HEIGHT);
            int val = 0;
            int argb = 0xFF;


            for (int x = 0; x < wm.GLOBAL_WIDTH; x++)
            {
                for (int y = 0; y < wm.GLOBAL_HEIGHT; y++)
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

        public static byte[] ConvertDBByteArray(byte[] from_db)
        {
            String str = Util.ByteArrayToString(from_db);
            byte[] data = new byte[str.Length / 2];

            for (int n = 0; n < str.Length / 2; n++)
            {
                data[n] = byte.Parse(str.Substring(n * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return data;
        }

        public static byte[] ConvertDBString(string str)
        {
            byte[] data = new byte[str.Length / 2];

            for (int n = 0; n < str.Length / 2; n++)
            {
                data[n] = byte.Parse(str.Substring(n * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }

            return data;
        }

        public static string GetStringFromUser(string prompt, int x, int y, TCODConsole con)
        {
            string temp = "";
            TCODKey key;
            while (true)
            {
                con.print(x-prompt.Length, y, prompt);
                con.print(x, y, temp);

                TCODConsole.flush();

                key = TCODConsole.waitForKeypress(true);
                if (key.KeyCode == TCODKeyCode.Escape)
                {
                    con.print(x - prompt.Length, y, "                                                                         ");
                    break;
                }

                if (key.Character.ToString() == "\0")
                {
                    continue;
                }

                if (key.KeyCode == TCODKeyCode.Backspace)
                {
                    if (temp.Length > 0)
                        temp = temp.Remove(temp.Length - 1);

                    con.print(x, y, new String(' ', temp.Length + 1));
                    continue;
                }

                if (key.KeyCode == TCODKeyCode.Enter)
                {
                    con.print(x - prompt.Length, y, "                                                                         ");
                    return temp;
                }

                temp += key.Character.ToString();
            }
            return null;
        }
    }


}
