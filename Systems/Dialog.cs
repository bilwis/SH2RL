using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using libtcod;

namespace ShootyShootyRL.Systems
{
    public class Dialog
    {
        public String Text;

        public Dialog(String text)
        {
            this.Text = text;
        }

        public void Render(TCODConsole con)
        {
            int maxchars = con.getWidth() - 4;
            int y = 2;

            con.setForegroundColor(TCODColor.darkerAzure);
            con.printFrame(0, 0, con.getWidth(), con.getHeight());
            con.setForegroundColor(TCODColor.white);

            foreach (String line in wrapLine(Text, maxchars))
            {
                con.print(2, y, line);
                y++;
            }
        }

        protected List<String> wrapLine(String line, int maxchars)
        {
            List<String> lines = new List<string>();
            string temp;

            if (line.Length > maxchars)
            {
                //Oh god the horror that is this function
                //
                //Further down, lines are printed from latest to newest (added to "lines")
                //so in order to display multiline messages correctly, the last of the multiple
                //lines must be added to lines first and the first last. This is done via 
                //a temporary array which is filled from highest to lowest and then added to lines.

                int templines = (int)Math.Ceiling((double)line.Length / (double)maxchars);
                string[] temparr = new string[templines];
                int k = 0;

                temp = line;
                while (temp.Length > maxchars)
                {
                    temparr[k] = temp.Substring(0, maxchars);
                    temp = temp.Remove(0, maxchars);
                    k++;
                }
                temparr[k] = temp;

                foreach (String s in temparr)
                {
                    lines.Add(s);
                }
            }
            else
            {
                lines.Add(line);
            }

            return lines;
        }
    }

    public class InputDialog : Dialog
    {
        SortedDictionary<char, string> responses;
        int selectedIndex;

        public InputDialog(String caption, SortedDictionary<char, string> resp):
            base(caption)
        {
            this.responses = resp;
            selectedIndex = 0;
        }

        public void MoveSelection(int step)
        {
            if (selectedIndex + step > responses.Count)
                selectedIndex = (selectedIndex + step) - responses.Count;
            if (selectedIndex + step < 0)
                selectedIndex = responses.Count - (selectedIndex+step);
        }

        public void Render(TCODConsole con, int x, int y, int width, int height)
        {

        }

        public int GetCurrentSelectionIndex()
        {
            return selectedIndex;
        }

        public int Confirm()
        {
            return selectedIndex;
        }

        public int SelectAndConfirm(char c)
        {
            char[] keys = responses.Keys.ToArray<char>();
            for (int i = 0; i < responses.Count; i++)
            {
                if (keys[i] == c)
                    return i;
            }

            return -1;
        }
    }
}
