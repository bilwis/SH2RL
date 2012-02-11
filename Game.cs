﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;

using libtcod;

using ShootyShootyRL.Mapping;
using ShootyShootyRL.Objects;

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
    //TODO: Fix dat Z-cell stuff!

    class Game
    {
        static int WINDOW_WIDTH = 100;
        static int WINDOW_HEIGHT = 80;
        static float MAIN_TO_STATUS_RATIO = 0.8f;

        static int MAIN_HEIGHT = (int)Math.Floor(WINDOW_HEIGHT * (MAIN_TO_STATUS_RATIO));
        static int STATUS_HEIGHT = (int)Math.Ceiling(WINDOW_HEIGHT * (1 - MAIN_TO_STATUS_RATIO));

        public bool MULTITHREADED_LOADING = true;

        TCODConsole root;
        TCODConsole status;
        TCODConsole main;

        SQLiteConnection dbconn; 

        bool endGame = false;

        Creature player;
        AICreature testai;
        WorldMap wm;
        uint seed;
        Map map;
        FactionManager facman;

        Random rand;

        int tar_x = -1, tar_y = -1, tar_z = -1;
        ulong turn = 1;
        ulong gameTurn = 1;

        public MessageHandler Out;

        public String ProfileName;
        public String ProfilePath;

        //DEBUG VARS
        string test_item_guid;
        string testai_guid;

        public Game()
        {
            //TCODConsole.setCustomFont("terminal12x12_gs_ro.png", (int)TCODFontFlags.LayoutAsciiInRow);
            TCODConsole.initRoot(WINDOW_WIDTH, WINDOW_HEIGHT, "ShootyShooty RL", false,TCODRendererType.SDL);
            TCODSystem.setFps(60);

            root = TCODConsole.root;
            status = new TCODConsole(WINDOW_WIDTH, STATUS_HEIGHT);
            main = new TCODConsole(WINDOW_WIDTH, MAIN_HEIGHT);

            Out = new MessageHandler();

            endGame = false;
        }

        private int menu()
        {
            //TODO: Do a proper menu system, this is just atrocious

            TCODSystem.setFps(10);
            int selected = 0;
            bool enter = false;
            TCODKey key;
            List<string> profiles = new List<string>();

            //Retrieve profile folders
            string[] temp;
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Game)).CodeBase);
            path = path.Remove(0, 6);
            path = System.IO.Path.Combine(path, "profiles");

            profiles = Directory.EnumerateDirectories(path).ToList<string>();
            for (int j = 0; j < profiles.Count; j++)
            {
                temp = profiles[j].Split('\\');
                profiles[j] = temp[temp.Length-1];
            }

            root.setForegroundColor(TCODColor.darkerLime);
            root.printFrame(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT);

            root.setBackgroundFlag(TCODBackgroundFlag.Set);

            root.setForegroundColor(TCODColor.lightestHan);
            root.setBackgroundColor(TCODColor.darkestHan);
            root.print(WINDOW_WIDTH / 2 - 16, 10, "ShootyShootyRoguelike (pre-alpha)");
            root.setBackgroundColor(TCODColor.black);
            root.print(WINDOW_WIDTH / 2 - 17, 11, "created by Clemens Curio ('bilwis')");
            root.print(WINDOW_WIDTH / 2 - 4, 12, "10022012");

            while (true)
            {
                root.setForegroundColor(TCODColor.white);
                root.setBackgroundColor(TCODColor.black);

                if (selected == 0)
                    root.setBackgroundColor(TCODColor.sepia);
                root.print(WINDOW_WIDTH / 2 - 5, WINDOW_HEIGHT / 2 - 1, "New Profile");
                root.setBackgroundColor(TCODColor.black);

                if (selected == 1)
                    root.setBackgroundColor(TCODColor.sepia);
                root.print(WINDOW_WIDTH / 2 - 5, WINDOW_HEIGHT / 2, "Load Profile");
                root.setBackgroundColor(TCODColor.black);

                if (selected == 2)
                    root.setBackgroundColor(TCODColor.sepia);
                root.print(WINDOW_WIDTH / 2 - 5, WINDOW_HEIGHT / 2 + 1, "Exit Game");
                root.setBackgroundColor(TCODColor.black);

                TCODConsole.flush();

                key = TCODConsole.waitForKeypress(true);

                if (key.KeyCode == TCODKeyCode.Up)
                {
                    if (selected > 0)
                        selected--;
                    else if (selected == 0)
                        selected = 2;
                }
                if (key.KeyCode == TCODKeyCode.Down)
                {
                    if (selected < 2)
                        selected++;
                    else if (selected == 2)
                        selected = 0;
                }
                if (key.KeyCode == TCODKeyCode.Enter)
                {
                    root.print(WINDOW_WIDTH / 2 - 5, WINDOW_HEIGHT / 2 - 1, "                ");
                    root.print(WINDOW_WIDTH / 2 - 5, WINDOW_HEIGHT / 2, "                ");
                    root.print(WINDOW_WIDTH / 2 - 5, WINDOW_HEIGHT / 2 + 1, "                ");

                    if (selected == 2)
                        return -1; //Exit 
                    if (selected == 0) //New Profile
                    {
                        //Enter Name, continue/new
                        ProfileName = Util.GetStringFromUser("Please enter your name: ", WINDOW_WIDTH / 2, WINDOW_HEIGHT / 2, root);
                        string seed_string = Util.GetStringFromUser("Please enter the map seed: ", WINDOW_WIDTH / 2, WINDOW_HEIGHT / 2, root);
                        if (!UInt32.TryParse(seed_string, out seed))
                            seed = (uint)seed_string.GetHashCode();
                        
                        return 0;
                    }

                    if (selected == 1) //Load Profile
                    {
                        //Display list of profiles, choose one, continue/load
                        profiles.Add("Back");
                        selected = 0;
                        while (true)
                        {
                            for (int i = 0; i < profiles.Count; i++)
                            {
                                if (selected == i)
                                    root.setBackgroundColor(TCODColor.sepia);

                                root.print((WINDOW_WIDTH / 2) - (profiles[i].Length / 2), (WINDOW_HEIGHT / 2) - (profiles.Count /2) + i, profiles[i]);

                                if (selected == i)
                                    root.setBackgroundColor(TCODColor.black);
                            }

                            TCODConsole.flush();

                            key = TCODConsole.waitForKeypress(true);

                            if (key.KeyCode == TCODKeyCode.Up)
                            {
                                if (selected > 0)
                                    selected--;
                                else if (selected == 0)
                                    selected = profiles.Count -1;
                            }
                            if (key.KeyCode == TCODKeyCode.Down)
                            {
                                if (selected < profiles.Count)
                                    selected++;
                                if (selected == profiles.Count)
                                    selected = 0;
                            }
                            if (key.KeyCode == TCODKeyCode.Enter)
                            {
                                if (profiles[selected] == "Back")
                                {
                                    root.rect(1, (WINDOW_HEIGHT / 2) - (profiles.Count / 2) -1, WINDOW_WIDTH - 2, profiles.Count + 1, true);
                                    selected = 0;
                                    break;
                                }

                                ProfileName = profiles[selected];
                                return 1;
                            }
                        }

                    }
                }
            }

            return -1;
        }

        public void Run()
        {
            //Stopwatch sw = new Stopwatch();

            int menu_in = menu();
            TCODSystem.setFps(60);

            if (menu_in == -1)
                return;
   
            if (menu_in == 0)
                InitNew(ProfileName, seed);
            if (menu_in == 1)
                InitLoad(ProfileName);

            Render();

            while (!endGame && !TCODConsole.isWindowClosed())
            {
                var key = TCODConsole.waitForKeypress(true);

                if (HandleInput(key))
                {
                    if (endGame)
                    {
                        dbconn.Close();
                        dbconn.Dispose();
                        break;
                    }
                    if (tar_x != player.X || tar_y != player.Y || tar_z != player.Z)
                    {
                        player.Move(tar_x, tar_y, tar_z, map);
                    }

                    while (player.Actions.Count != 0)
                    {
                        Tick();
                    }
                    Render();

                    turn++;
                }
            }
        }

        public void InitLoad(string profile_name)
        {
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Game)).CodeBase);
            path = path.Remove(0, 6);
            string newPath = System.IO.Path.Combine(path, "profiles", profile_name);
            ProfilePath = newPath;
            InitDB(newPath);



        }

        public void InitNew(string profile_name, uint map_seed)
        {
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Game)).CodeBase);
            path = path.Remove(0, 6);
            string newPath = System.IO.Path.Combine(path, "profiles", profile_name);
            Directory.CreateDirectory(newPath);

            ProfilePath = newPath;

            InitDB(newPath);
            
            rand = new Random();

            facman = new FactionManager();
            Faction player_faction = new Faction("Player", "The player");
            player_faction.Init(facman);

            //Faction human_faction = new Faction("Humans", "The surviors of their self-made cathastrophe.");
            //Faction test_faction = new Faction("TESTFAC", "TEST FACTION PLEASE IGNORE");
            //human_faction.Init(facman);
            //test_faction.Init(facman);

            //human_faction.AddRelation(test_faction, FactionRelation.Hostile);
            //test_faction.AddRelation(human_faction, FactionRelation.Hostile);

            root.setForegroundColor(TCODColor.grey);
            root.setBackgroundColor(TCODColor.grey);
            root.setBackgroundFlag(TCODBackgroundFlag.Set);
            root.printFrame(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT);
            root.setForegroundColor(TCODColor.black);
            root.print(WINDOW_WIDTH / 2 - 9, WINDOW_HEIGHT / 2, "Map is generating...");
            string seed_string = "Seed: " + map_seed;
            root.print(WINDOW_WIDTH / 2 - (seed_string.Length/2), WINDOW_HEIGHT / 2 - 1, seed_string);
            TCODConsole.flush();
            root.setBackgroundFlag(TCODBackgroundFlag.Default);

            player = new Player(1300, 1300, 35, "Player", "A ragged and scruffy-looking individual.", '@');
            player.Init(TCODColor.yellow, Out, player_faction, new Objects.Action(ActionType.Idle, null, player, 0.0d));

            Out.SendMessage("Welcome to [insert game name here]!", Message.MESSAGE_WELCOME);
            Out.SendMessage("You wake up in a damp and shoddy shack. Or maybe a patch of dirt. Depends on the games' version.");

            wm = new WorldMap(map_seed, dbconn);
            map = new Map(player, wm, Out, facman, dbconn);

            RenderLoadingScreen();

            player.SetPosition(1300,1300, map.DropObject(1300, 1300, 35) +1);

            //testai = new AICreature(302, 300, 15, "TEST", "TEST CREATURE PLEASE IGNORE", 'A');
            //testai.Init(TCODColor.orange, Out, test_faction, new Objects.Action(ActionType.Idle, null, testai, 0.0d), new WalkerAI(rand.Next(0, 100000000)), map);
            //testai_guid = testai.GUID;
            //AICreature testai2 = new AICreature(290, 290, 15, "TEST2", "TEST CREATURE PLEASE IGNORE", 'B');
            //testai2.Init(TCODColor.orange, Out, test_faction, new Objects.Action(ActionType.Idle, null, testai, 0.0d), new WalkerAI(rand.Next(0, 10000000)), map);
            //map.AddCreature(testai);
            //map.AddCreature(testai2);

            map.AddCreature(player);

            Item test_item = new Item(1299, 1299, map.DropObject(1299, 1299, 35), "Shimmering rock", "A shining polished rock which seems to change color when you look at it.", (char)'*');
            test_item_guid = test_item.GUID;
            test_item.Init(TCODColor.red, Out);
            map.AddItem(test_item);
        }

        public void Save()
        {
            FileStream fstream = new FileStream(System.IO.Path.Combine(ProfilePath + "save.dat"), FileMode.Create);
            StreamWriter swriter = new StreamWriter(fstream);

            swriter.WriteLine("Version=pre1");
            swriter.WriteLine("MapSeed=" + seed.ToString());

            //Deserialize player


            //swriter.WriteLine("PlayerData=")
            //swriter.WriteLine(
        }

        private void RenderLoadingScreen()
        {
            TCODSystem.setFps(5);

            int step = 0;
            while (!map.initialized)
            {
                TCODConsole.checkForKeypress();
                root.setForegroundColor(TCODColor.black);
                root.setBackgroundColor(TCODColor.grey);
                root.setBackgroundFlag(TCODBackgroundFlag.Set);
                switch (step)
                {
                    case 0:
                        root.print(WINDOW_WIDTH / 2 + 8, WINDOW_HEIGHT / 2, " ");
                        root.print(WINDOW_WIDTH / 2 + 9, WINDOW_HEIGHT / 2, " ");
                        root.print(WINDOW_WIDTH / 2 + 10, WINDOW_HEIGHT / 2, " ");
                        break;
                    case 1:
                        root.print(WINDOW_WIDTH / 2 + 8, WINDOW_HEIGHT / 2, ".");
                        break;
                    case 2:
                        root.print(WINDOW_WIDTH / 2 + 9, WINDOW_HEIGHT / 2, ".");
                        break;
                    case 3:
                        root.print(WINDOW_WIDTH / 2 + 10, WINDOW_HEIGHT / 2, ".");
                        step = -1;
                        break;
                }

                step++;
                TCODConsole.flush();
                continue;

            }

            TCODSystem.setFps(60);
            root.setBackgroundFlag(TCODBackgroundFlag.Default);
        }

        public void InitDB(string path)
        {
            dbconn = new SQLiteConnection();
            dbconn.ConnectionString = "Data Source=" + path + "\\objects.db";
            dbconn.Open();

            SQLiteCommand command = new SQLiteCommand(dbconn);

            //Tiles Table
            command.CommandText = "CREATE TABLE IF NOT EXISTS tiles (guid BLOB NOT NULL PRIMARY KEY, data BLOB NOT NULL, fore BLOB NOT NULL, back BLOB NOT NULL);";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS tile_mapping (id BLOB NOT NULL PRIMARY KEY, guid BLOB NOT NULL);";
            command.ExecuteNonQuery();

            //Items Table
            command.CommandText = "CREATE TABLE IF NOT EXISTS items (guid BLOB NOT NULL PRIMARY KEY, data BLOB NOT NULL, color BLOB NOT NULL);";
            command.ExecuteNonQuery();

            //AI Creature Table
            //(TCODColor color, MessageHandler msg, Faction fac, AI ai, Map m)
            command.CommandText = "CREATE TABLE IF NOT EXISTS ai_creatures (guid BLOB NOT NULL PRIMARY KEY, data BLOB NOT NULL, faction_id BLOB NOT NULL, ai_id BLOB NOT NULL, color BLOB NOT NULL);";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS ai (guid BLOB NOT NULL PRIMARY KEY, data BLOB NOT NULL);";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS factions (guid BLOB NOT NULL PRIMARY KEY, data BLOB NOT NULL);";
            command.ExecuteNonQuery();

            //Cell content Table
            command.CommandText = "CREATE TABLE IF NOT EXISTS cell_contents (cell_id BLOB NOT NULL, content_type BLOB NOT NULL, content_guid BLOB NOT NULL PRIMARY KEY);";
            command.ExecuteNonQuery();

            //Cell tile changes Table
            command.CommandText = "CREATE TABLE IF NOT EXISTS diff_map (cell_id BLOB NOT NULL, abs_x BLOB, abs_y BLOB, abs_z BLOB, tile BLOB);";
            command.ExecuteNonQuery();

            command.Dispose();
        }
        
        public void Tick()
        {
            if (gameTurn == ulong.MaxValue - 1)
            {
                Out.SendMessage("You have reached Game Turn " + ulong.MaxValue + ". This is practially impossible. The developers' estimates show that using a conservative measure of just 17.1ms per turn, by the time you reached this turn, the sun had burned out approximately three million years ago. This is physicially impossible. The game will now crash and the universe collapse shortly thereafter.", Message.MESSAGE_ERROR);
                Render();
                TCODConsole.waitForKeypress(true);
                throw new UniversalSpaceTimeException();
            }

            player.Tick();
            map.Tick();

            gameTurn++;
            
        }

        public bool HandleInput(TCODKey key)
        {
            #region "Player Input"
            tar_y = player.Y;
            tar_x = player.X;
            tar_z = player.Z;

            switch (key.KeyCode)
            {
                case TCODKeyCode.KeypadEight:
                    tar_y = player.Y - 1;
                    return true;
                    break;
                case TCODKeyCode.KeypadSix:
                    tar_x = player.X + 1;
                    return true;
                    break;
                case TCODKeyCode.KeypadTwo:
                    tar_y = player.Y + 1;
                    return true;
                    break;
                case TCODKeyCode.KeypadFour:
                    tar_x = player.X - 1;
                    return true;
                    break;
                case TCODKeyCode.KeypadNine:
                    tar_y = player.Y - 1;
                    tar_x = player.X + 1;
                    return true;
                    break;
                case TCODKeyCode.KeypadThree:
                    tar_y = player.Y + 1;
                    tar_x = player.X + 1;
                    return true;
                    break;
                case TCODKeyCode.KeypadOne:
                    tar_y = player.Y + 1;
                    tar_x = player.X - 1;
                    return true;
                    break;
                case TCODKeyCode.KeypadSeven:
                    tar_y = player.Y - 1;
                    tar_x = player.X - 1;
                    return true;
                    break;
                case TCODKeyCode.KeypadFive:
                    return true;
                    break;

            }

            switch (key.Character)
            {
                case '<':
                    tar_z = player.Z - 1;
                    return true;
                    break;
                case '>':
                    tar_z = player.Z + 1;
                    return true;
                    break;
            }

            #endregion

            if (key.KeyCode == TCODKeyCode.F1)
            {
                tar_x = 300;
                tar_y = 300;
                tar_z = map.DropObject(300, 300, 28);
                return true;
            }
            

            if (key.KeyCode == TCODKeyCode.F2)
            {
                tar_x = 300;
                tar_y = 300;
                tar_z = map.DropObject(300, 300, 12);
                return true;
            }

            if (key.KeyCode == TCODKeyCode.F12)
            {
                GC.Collect();
            }
            

            if (key.KeyCode == TCODKeyCode.Escape)
            {
                endGame = true;
            }
            if (key.KeyCode != TCODKeyCode.NoKey)
            {
                TCODConsole.root.print(0, 0, key.Character.ToString());
                TCODConsole.root.print(0, 1, key.KeyCode.ToString());
                return false;
            }

            return false;
        }

        public void Render()
        {
            main.setForegroundColor(TCODColor.darkerLime);
            main.printFrame(0, 0, WINDOW_WIDTH, MAIN_HEIGHT);

            map.Render(main, 1, 1, WINDOW_WIDTH-2, MAIN_HEIGHT-2);
            main.setForegroundColor(TCODColor.white);
            main.print(2, 0, "Turn: " + turn + " | Gameturn: " + gameTurn);
            main.print(2, 1, "Z_LEVEL: " + player.Z);

            if (map.initialized)
                main.setForegroundColor(TCODColor.green);
            else
                main.setForegroundColor(TCODColor.red);

            main.print(WINDOW_WIDTH - 1, 0, "+");

            TCODConsole.blit(main, 0, 0, WINDOW_WIDTH, MAIN_HEIGHT, root, 0, 0);

            status.setForegroundColor(TCODColor.darkerOrange);
            status.setBackgroundColor(TCODColor.brass);
            status.setBackgroundFlag(TCODBackgroundFlag.Set);
            status.printFrame(0, 0, WINDOW_WIDTH, STATUS_HEIGHT);

            status.setBackgroundFlag(TCODBackgroundFlag.Default);
            status.setForegroundColor(TCODColor.darkerGrey);

            Out.Render(status); //Print the message log, y'all
            TCODConsole.blit(status, 0, 0, WINDOW_WIDTH, STATUS_HEIGHT, root, 0, MAIN_HEIGHT);

            TCODConsole.flush();

        }
    }
}

