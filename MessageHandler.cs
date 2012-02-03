using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libtcod;

namespace ShootyShootyRL
{
    [Serializable()]
    public class Message
    {
        public String MessageText;
        public TCODColor ForeColor;
        public TCODColor BackColor;

        public static MessageColor MESSAGE_ERROR = new MessageColor(TCODColor.red, TCODColor.darkerCrimson);
        public static MessageColor MESSAGE_DEFAULT = new MessageColor(TCODColor.darkerGrey, null);
        public static MessageColor MESSAGE_DEBUG = new MessageColor(null, null);
        public static MessageColor MESSAGE_WELCOME = new MessageColor(TCODColor.darkGreen, TCODColor.darkYellow);

        public Message(String text, TCODColor fore, TCODColor back)
        {
            MessageText = text;
            ForeColor = fore;
            BackColor = back;
        }

        public Message(String text, MessageColor color)
        {
            MessageText = text;
            ForeColor = color.ForeColor;
            BackColor = color.BackColor;
        }

        public MessageColor GetMessageColor()
        {
            return new MessageColor(ForeColor, BackColor);
        }
    }

    [Serializable()]
    public class MessageColor
    {
        public TCODColor ForeColor;
        public TCODColor BackColor;

        public MessageColor(TCODColor fore, TCODColor back)
        {
            ForeColor = fore;
            BackColor = back;
        }
    }

    [Serializable()]
    public class MessageHandler
    {
        List<Message> log;

        public MessageHandler()
        {
            log = new List<Message>();
        }

        public void SendMessage(String text, MessageColor color)
        {
            if (color != Message.MESSAGE_DEBUG)
            {
                log.Add(new Message(text, color));
                return;
            }

            System.Console.WriteLine(text);
        }

        public void SendMessage(String text, TCODColor fore, TCODColor back)
        {
            log.Add(new Message(text, new MessageColor(fore, back)));
        }

        public void SendMessage(String text)
        {
            log.Add(new Message(text, Message.MESSAGE_DEFAULT));
        }

        public void SendDebugMessage(String text)
        {
            System.Console.WriteLine(text);
        }

        public void Render(TCODConsole con, bool linewrap = true)
        {
            int maxlines = con.getHeight()-2;   //Corrected for border
            int maxchars = con.getWidth()-2;
            List<String> lines = new List<string>();
            List<MessageColor> colors = new List<MessageColor>();
            string temp;

            if (log.Count == 0)
                return;

            int i = log.Count-maxlines-1;
            if (log.Count <= maxlines)
                i = 0;
            while (i < log.Count)
            {
                if (log[i].MessageText.Length > maxchars && linewrap)
                {
                    //Oh god the horror that is this function
                    //
                    //Further down, lines are printed from latest to newest (added to "lines")
                    //so in order to display multiline messages correctly, the last of the multiple
                    //lines must be added to lines first and the first last. This is done via 
                    //a temporary array which is filled from highest to lowest and then added to lines.
                    int templines =(int)Math.Ceiling((double)log[i].MessageText.Length / (double)maxchars);
                    string[] temparr = new string[templines];
                    int k = templines-1;
                    
                    temp = log[i].MessageText;
                    while (temp.Length > maxchars)
                    {
                        temparr[k] = temp.Substring(0, maxchars);
                        colors.Add(log[i].GetMessageColor());
                        temp = temp.Remove(0, maxchars);
                        k--;
                    }
                    temparr[k] = temp;

                    foreach (String s in temparr)
                    {
                        lines.Add(s);
                    }

                    colors.Add(log[i].GetMessageColor());
                }
                else 
                {
                    lines.Add(log[i].MessageText);
                    colors.Add(log[i].GetMessageColor());
                }
                i++;
            }

            int endcount = lines.Count - maxlines;
            if (lines.Count < maxlines)
                endcount = 0;
            int y = 0;
            for (int j = lines.Count-1; j >= endcount; j--)
            {
                con.setForegroundColor(colors[j].ForeColor);
                con.setBackgroundFlag(TCODBackgroundFlag.None);
                if (colors[j].BackColor != null)
                {
                    con.setBackgroundColor(colors[j].BackColor);
                    con.setBackgroundFlag(TCODBackgroundFlag.Screen);
                }

                con.print(1, 1 + y, lines[j]);
                y++;
            }
        }
    }
}
