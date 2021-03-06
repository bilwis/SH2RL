﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Data.SQLite;
using System.Runtime.Serialization.Formatters.Binary;

using libtcod;

using ShootyShootyRL.Mapping;
using ShootyShootyRL.Objects;
using ShootyShootyRL.Systems;
using ShootyShootyRL.Objects.Bodies;
//using ShootyShootyRL.Systems.Forms;

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
    enum MainDisplayMode
    {
        Game = 0,
        Inventory = 1,
        Character = 2,
        Crafting = 3,
        Log = 4,
        Cursor = 5
    }

    enum SelectionMode
    {
        Item = 0,
        Creature = 1
    }

    class Game
    {
        static string currentVersionCode = "pre2";

        static int WINDOW_WIDTH = 130;
        static int WINDOW_HEIGHT = (int)3 * WINDOW_WIDTH / 4; //4:3 Aspect
        static float MAIN_TO_STATUS_RATIO = 0.75f;
        static float DIALOG_RATIO = 0.175f;
        static float STATUS_TO_MESSAGES_RATIO = 0.275f;

        static int DIALOG_HEIGHT = (int)Math.Ceiling(WINDOW_HEIGHT * (DIALOG_RATIO));
        static int MAIN_HEIGHT = (int)Math.Floor(WINDOW_HEIGHT * (MAIN_TO_STATUS_RATIO));
        static int STATUS_HEIGHT = (int)Math.Ceiling(WINDOW_HEIGHT * (1 - MAIN_TO_STATUS_RATIO));

        static float EFFECT_RATE_MS = 50f;
        static float EFFECTS_ALPHA = 0.8f;

        static string BODY_DEF_HUMAN = "body_human.xml";

        public bool MULTITHREADED_LOADING = true;

        TCODConsole root;

        TCODConsole status;
        TCODConsole messages;

        TCODConsole main;
        TCODConsole dialog;
        TCODConsole effects;

        MainDisplayMode mdm = MainDisplayMode.Game;

        SQLiteConnection dbconn;

        bool endGame = false;

        Player player;
        AICreature testai;
        Creature dummy;

        Firearm testgun;
        Magazine testmag;

        WorldMap wm;
        uint seed;
        Map map;
        FactionManager facman;

        Random rand;

        int tar_x = -1, tar_y = -1, tar_z = -1;
        int cur_rel_x = 0, cur_rel_y = 0;

        int menu_selection = 0;

        bool player_pickup = false;
        bool player_equip = false;
        bool player_unequip = false;
        bool player_attack_ranged = false;

        bool lock_cursor = false;

        ulong turn = 1;
        ulong gameTurn = 1;

        public MessageHandler Out;

        public String ProfileName;
        public String ProfilePath;

        System.Object current_dialog = null;

        //DEBUG VARS
        string test_item_guid;
        string testai_guid;
        ParticleEmitter emit = new ParticleEmitter(1300, 1300, 31, 1.5f, 70f, 0.75f, 1.0f, -2f, 2f);
        int particle_count = 0;

        public Game()
        {
            //TCODConsole.setCustomFont("terminal12x12_gs_ro.png", (int)TCODFontFlags.LayoutAsciiInRow);
            TCODSystem.forceFullscreenResolution(1680, 1050);
            TCODConsole.initRoot(WINDOW_WIDTH, WINDOW_HEIGHT, "ShootyShooty RL", false, TCODRendererType.GLSL);
            TCODSystem.setFps(60);

            //TCODConsole.setFullscreen(true);

            root = TCODConsole.root;

            main = new TCODConsole(WINDOW_WIDTH, MAIN_HEIGHT);

            status = new TCODConsole((int)(WINDOW_WIDTH * STATUS_TO_MESSAGES_RATIO), STATUS_HEIGHT);
            //statsPanel = new Panel((int)(WINDOW_HEIGHT * MAIN_TO_STATUS_RATIO), 0, WINDOW_WIDTH, STATUS_HEIGHT, ref status);

            dialog = new TCODConsole(WINDOW_WIDTH, DIALOG_HEIGHT);
            //dialogPanel = new Panel(0, 0, WINDOW_WIDTH, DIALOG_HEIGHT, ref dialog);

            messages = new TCODConsole((int)Math.Ceiling(WINDOW_WIDTH * (1.0f - STATUS_TO_MESSAGES_RATIO)), STATUS_HEIGHT);
            effects = new TCODConsole(WINDOW_WIDTH, MAIN_HEIGHT);

            emit.Init(TCODColor.orange, 20);

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
                profiles[j] = temp[temp.Length - 1];
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

                if (key.Character == 'r')
                {
                    Random rand = new Random();
                    ProfileName = rand.Next(0, 1000000).ToString();
                    seed = (uint)rand.Next(0, 1000000).ToString().GetHashCode();
                    return 0;
                }
                if (key.KeyCode == TCODKeyCode.KeypadSubtract)
                {
                    if (selected > 0)
                        selected--;
                    else if (selected == 0)
                        selected = 2;
                }
                if (key.KeyCode == TCODKeyCode.KeypadAdd)
                {
                    if (selected < 2)
                        selected++;
                    else if (selected == 2)
                        selected = 0;
                }
                if (key.KeyCode == TCODKeyCode.KeypadEnter || key.KeyCode == TCODKeyCode.Enter)
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

                                root.print((WINDOW_WIDTH / 2) - (profiles[i].Length / 2), (WINDOW_HEIGHT / 2) - (profiles.Count / 2) + i, profiles[i]);

                                if (selected == i)
                                    root.setBackgroundColor(TCODColor.black);
                            }

                            TCODConsole.flush();

                            key = TCODConsole.waitForKeypress(true);

                            if (key.KeyCode == TCODKeyCode.KeypadSubtract)
                            {
                                if (selected > 0)
                                    selected--;
                                else if (selected == 0)
                                    selected = profiles.Count - 1;
                            }
                            if (key.KeyCode == TCODKeyCode.KeypadAdd)
                            {
                                if (selected < profiles.Count)
                                    selected++;
                                if (selected == profiles.Count)
                                    selected = 0;
                            }
                            if (key.KeyCode == TCODKeyCode.KeypadEnter || key.KeyCode == TCODKeyCode.Enter)
                            {
                                if (profiles[selected] == "Back")
                                {
                                    root.rect(1, (WINDOW_HEIGHT / 2) - (profiles.Count / 2) - 1, WINDOW_WIDTH - 2, profiles.Count + 1, true);
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

        public void DisplayDialog(String text)
        {
            current_dialog = new Dialog(text);
            RenderAll();
        }

        public int DisplayInputDialog(String caption, SortedDictionary<char, string> responses)
        {
            current_dialog = new InputDialog(caption, responses);
            InputDialog d = (InputDialog)current_dialog;
            RenderAll();

            int selection = -1;
            bool abort = false;

            while (!abort)
            {
                var key = TCODConsole.waitForKeypress(true);

                if (key.Character == 'q')
                {
                    current_dialog = null;
                    RenderAll();
                    break;
                }

                if (responses.ContainsKey(key.Character))
                {
                    selection = d.SelectAndConfirm(key.Character);
                    break;
                }

                switch (key.KeyCode)
                {
                    case TCODKeyCode.KeypadAdd:
                        d.MoveSelection(1);
                        break;
                    case TCODKeyCode.KeypadSubtract:
                        d.MoveSelection(-1);
                        break;
                    case TCODKeyCode.KeypadEnter:
                        selection = d.Confirm();
                        break;
                    case TCODKeyCode.Enter:
                        selection = d.Confirm();
                        break;
                    default:
                        abort = true;
                        break;
                }

                if (abort)
                {
                    HandleInput(key);
                    d = null;
                }
                if (selection != -1)
                {
                    d = null;
                    abort = true;
                }

                current_dialog = d;
                RenderAll();
            }


            return selection;
        }

        public void CancelDialog()
        {
            current_dialog = null;
            RenderAll();

        }

        private void moveCursor(int tar_x, int tar_y)
        {
            if (cur_rel_x + tar_x > 0 && cur_rel_x + tar_x < WINDOW_WIDTH - 2)
            {
                cur_rel_x += tar_x;
            }
            if (cur_rel_y + tar_y > 0 && cur_rel_y + tar_y < MAIN_HEIGHT)
            {
                cur_rel_y += tar_y;
            }
        }

        public void Run()
        {
            //Stopwatch sw = new Stopwatch();

            int menu_in = menu();

            if (menu_in == -1)
                return;

            if (menu_in == 0)
                InitNew(ProfileName, seed);
            if (menu_in == 1)
                InitLoad(ProfileName);

            emit.Tick(0);
            Tick();
            RenderAll();

            bool redraw = false;

            TCODSystem.setFps(30);
            TCODConsole.setKeyboardRepeat(200, 60);
            Stopwatch effects_timer = new Stopwatch();
            effects_timer.Start();
            while (!endGame && !TCODConsole.isWindowClosed())
            {
                //RenderAll loop

                var key = TCODConsole.checkForKeypress(1);

                if (key.KeyCode != TCODKeyCode.NoKey)
                {
                    if (HandleInput(key))
                    {
                        if (endGame)
                        {
                            Save();
                            dbconn.Close();
                            dbconn.Dispose();
                            break;
                        }
                        
                        if (mdm == MainDisplayMode.Game && (tar_x != 0 || tar_y != 0 || tar_z != 0))
                        {
                            tar_x += player.X;
                            tar_y += player.Y;
                            tar_z += player.Z;

                            player.Move(tar_x, tar_y, tar_z, map);
                        }

                        if (player_pickup)
                            playerPickup();

                        if (player_equip)
                            playerEquip();

                        if (player_unequip)
                            playerUnequip();

                        if (player_attack_ranged)
                            playerAttackRanged(); //TODO: Selecting stuff (perhaps a out of game loop "select()" function ?)

                        Stopwatch light = new Stopwatch();
                        light.Start();

                        while (player.Actions.Count != 0)
                        {
                            Tick();
                        }

                        light.Stop();
                        //Out.SendMessage("Ticking through to next action took " + light.ElapsedMilliseconds + "ms.", Message.MESSAGE_INFO);

                        redraw = true;
                        turn++;
                    }
                }

                if (redraw)
                {
                    RenderAll();
                    redraw = false;
                }


                if (effects_timer.ElapsedMilliseconds >= EFFECT_RATE_MS)
                {
                    //if (emit.abs_z == player.Z)
                    //    emit.Tick((int)effects_timer.ElapsedMilliseconds);

                    //RenderEffects();
                    effects_timer.Restart();
                }
            }
        }

        public Object Select(SelectionMode mode, int abs_cur_x = 0, int abs_cur_y = 0)
        {
            mdm = MainDisplayMode.Cursor;
            cur_rel_x = WINDOW_WIDTH / 2;
            cur_rel_y = MAIN_HEIGHT / 2;
            lock_cursor = false;

            bool redraw = true;

            //Cursor position
            while (!lock_cursor)
            {
                var key = TCODConsole.checkForKeypress(1);

                if (key.KeyCode != TCODKeyCode.NoKey)
                {
                    if (HandleInput(key))
                    {
                        if (mdm == MainDisplayMode.Cursor)
                        {
                            moveCursor(tar_x, tar_y);
                        }


                        redraw = true;
                        turn++;
                    }
                }

                if (redraw)
                {
                    RenderAll();
                    redraw = false;
                }
                //if (effects_timer.ElapsedMilliseconds >= EFFECT_RATE_MS)
                //{
                //    //if (emit.abs_z == player.Z)
                //    //    emit.Tick((int)effects_timer.ElapsedMilliseconds);

                //    //RenderEffects();
                //    effects_timer.Restart();
                //}
            }

            mdm = MainDisplayMode.Game;

            int cur_abs_x, cur_abs_y;

            cur_abs_x = player.X - (WINDOW_WIDTH / 2) + cur_rel_x;
            cur_abs_y = player.Y - (MAIN_HEIGHT / 2) + cur_rel_y;

            int ch_int = 97;

            switch (mode)
            {
                case SelectionMode.Creature:
                    List<Creature> c_list = new List<Creature>();
                    c_list = map.ComposeCreatures(cur_abs_x, cur_abs_y, player.Z);
                    if (c_list.Count == 0)
                        return null;
                    if (c_list.Count == 1)
                        return c_list[0];

                    SortedDictionary<char, string> sel = new SortedDictionary<char, string>();
                    for (int i = 0; i < c_list.Count; i++)
                    {
                        sel.Add((char)(ch_int+i), c_list[i].Name);
                    }
                    int resp = DisplayInputDialog("Select creature: ", sel);
                    return c_list[resp];

                case SelectionMode.Item:
                    List<Item> i_list = new List<Item>();
                    i_list = map.ComposeItems(cur_abs_x, cur_abs_y, player.Z);
                    if (i_list.Count == 0)
                        return null;
                    if (i_list.Count == 1)
                        return i_list[0];

                    SortedDictionary<char, string> sel_i = new SortedDictionary<char, string>();
                    for (int i = 0; i < i_list.Count; i++)
                    {
                        sel_i.Add((char)(ch_int+i), i_list[i].Name);
                    }
                    int resp_i = DisplayInputDialog("Select item: ", sel_i);
                    return i_list[resp_i];
            }

            return null;
        }

        public void InitLoad(string profile_name)
        {
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Game)).CodeBase);
            path = path.Remove(0, 6);
            string newPath = System.IO.Path.Combine(path, "profiles", profile_name);
            ProfilePath = newPath;

            //Initialize the database(s)
            InitDB(newPath);

            facman = new FactionManager();
            rand = new Random();

            //Load the data from the save.dat
            FileStream fstream = new FileStream(System.IO.Path.Combine(ProfilePath, "save.dat"), FileMode.Open);
            StreamReader sreader = new StreamReader(fstream);

            string pdat = "00", fdat = "00";
            int[] wm_param = new int[7];
            string[] temp;
            while (!sreader.EndOfStream)
            {
                temp = sreader.ReadLine().Split('=');
                switch (temp[0])
                {
                    case "Version":
                        if (temp[1] != currentVersionCode)
                            throw new Exception("Error while loading profile: save.dat not at current version");
                        break;
                    case "MapSeed":
                        if (!UInt32.TryParse(temp[1], out seed))
                            throw new Exception("Error while loading profile: MapSeed is not a number.");
                        break;
                    case "FactionData":
                        fdat = temp[1];
                        break;
                    case "PlayerData":
                        pdat = temp[1];
                        break;
                    case "CellWidth":
                        if (!Int32.TryParse(temp[1], out wm_param[0]))
                            throw new Exception("Error while loading profile: CellWidth is not a number.");
                        break;
                    case "CellHeight":
                        if (!Int32.TryParse(temp[1], out wm_param[1]))
                            throw new Exception("Error while loading profile: CellHeight is not a number.");
                        break;
                    case "CellDepth":
                        if (!Int32.TryParse(temp[1], out wm_param[2]))
                            throw new Exception("Error while loading profile: CellDepth is not a number.");
                        break;
                    case "CellsX":
                        if (!Int32.TryParse(temp[1], out wm_param[3]))
                            throw new Exception("Error while loading profile: CellsX is not a number.");
                        break;
                    case "CellsY":
                        if (!Int32.TryParse(temp[1], out wm_param[4]))
                            throw new Exception("Error while loading profile: CellsY is not a number.");
                        break;
                    case "CellsZ":
                        if (!Int32.TryParse(temp[1], out wm_param[5]))
                            throw new Exception("Error while loading profile: CellsZ is not a number.");
                        break;
                    case "GroundLevel":
                        if (!Int32.TryParse(temp[1], out wm_param[6]))
                            throw new Exception("Error while loading profile: GroundLevel is not a number.");
                        break;
                }
            }

            //Deserialize player
            Faction _fac;

            byte[] data;

            MemoryStream mstream = new MemoryStream();
            BinaryFormatter deserializer = new BinaryFormatter();

            data = Util.ConvertDBString(fdat);

            //Write the converted byte array into the MemoryStream (and reset it to origin)
            mstream.Write(data, 0, data.Length);
            mstream.Seek(0, SeekOrigin.Begin);

            //Deserialize the faction object itself
            _fac = (Faction)deserializer.Deserialize(mstream);

            _fac.Init(facman);

            mstream.SetLength(0);
            mstream.Seek(0, SeekOrigin.Begin);

            data = Util.ConvertDBString(pdat);

            //Write the converted byte array into the MemoryStream (and reset it to origin)
            mstream.Write(data, 0, data.Length);
            mstream.Seek(0, SeekOrigin.Begin);

            //Deserialize the player
            player = (Player)deserializer.Deserialize(mstream);

            player.Init(TCODColor.yellow, Out, _fac, new Objects.Action(ActionType.Idle, null, player, 0.0d));

            //SETUP MAP AND WORLDMAP, ADD PLAYER
            wm = new WorldMap(seed, dbconn, wm_param);
            map = new Map(player, wm, Out, facman, dbconn, MAIN_HEIGHT - 2, WINDOW_WIDTH - 2);

            RenderLoadingScreen();

            player.SetPosition(player.X, player.Y, map.DropObject(player.X, player.Y, player.Z + 1));
            map.AddCreature(player);
            Out.SendMessage("Profile " + ProfileName + " successfully loaded. Last played: " + File.GetLastWriteTime(Path.Combine(ProfilePath, "save.dat")) + ".");

            mstream.Close();
            sreader.Close();
            fstream.Close();
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
            Faction test_faction = new Faction("TESTFAC", "TEST FACTION PLEASE IGNORE");
            //human_faction.Init(facman);
            test_faction.Init(facman);

            //human_faction.AddRelation(test_faction, FactionRelation.Hostile);
            test_faction.AddRelation(player_faction, FactionRelation.Hostile);

            root.setBackgroundFlag(TCODBackgroundFlag.Default);

            player = new Player(1300, 1300, 35, "Player", "A ragged and scruffy-looking individual.", '@', new Body(BODY_DEF_HUMAN), new CharStats(10, 10, 10));

            player.Init(TCODColor.yellow, Out, player_faction, new Objects.Action(ActionType.Idle, null, player, 0.0d));

            Out.SendMessage("Welcome to [insert game name here]!", Message.MESSAGE_WELCOME);
            Out.SendMessage("You wake up in a damp and shoddy shack. Or maybe a patch of dirt. Depends on the games' version.");

            //Default map parameters
            int[] parameters = new int[7];
            parameters[0] = 200; //CellWidth
            parameters[1] = 200; //CellHeight
            parameters[2] = 20; //CellDepth

            parameters[3] = 10; //CellsX
            parameters[4] = 10; //CellsY
            parameters[5] = 6; //CellsZ

            parameters[6] = 45; //GroundLevel

            wm = new WorldMap(map_seed, dbconn, parameters);
            map = new Map(player, wm, Out, facman, dbconn, MAIN_HEIGHT - 2, WINDOW_WIDTH - 2);

            RenderLoadingScreen();

            player.SetPosition(player.X, player.Y, map.DropObject(player.X, player.Y, player.Z + 1));
            map.AddCreature(player);

            //testai = new AICreature(1301, 1301, 31, "TEST", "TEST CREATURE PLEASE IGNORE", 'A', new Body(BODY_DEF_HUMAN), new CharStats(10,10,10));
            //testai.Init(TCODColor.orange, Out, test_faction, new Objects.Action(ActionType.Idle, null, testai, 0.0d), new WalkerAI(rand.Next(0, 100000000)), map);
            //testai_guid = testai.GUID;
            //map.AddCreature(testai);

            dummy = new Creature(1299, 1299, 31, "TEST", "A Training Dummy.", 'D', new Body(BODY_DEF_HUMAN), new CharStats(10, 10, 10));
            dummy.Init(TCODColor.turquoise, Out);
            map.AddCreature(dummy);

            List<FireMode> gl_fm = new List<FireMode>();
            gl_fm.Add(FireMode.Semi);
            gl_fm.Add(FireMode.Burst2);
            testgun = new Firearm(1299, 1301, 31, "Glock 17C", "A semi-automatic pistol manufactured by Austrian Glock Ges. m.b.H. Large parts of it are manufactured from extremely resilient high-strengh polymer and it chambers NATO standard 9x19mm Parabellum rounds. It has a magazine capacity of 17 rounds. The C Variant features slots cut into the barrel and slide compensating for muzzle rise and recoil. It is one of the most commonly used firearms in US law enforcement and enjoys great popularity in the gun community.", 'F', 5.0d, new Caliber(9d, 19d), GunType.Pistol, gl_fm, 17);
            testgun.Init(TCODColor.grey, Out);

            testmag = new Magazine(1301, 1299, 31, "9mm Clip", "TEST MAG PLEASE IGNORE.", 'M', 0.75d, 30, 30, new Caliber(5.56d, 45.0d), AmmoModifier.Regular);
            testmag.Init(TCODColor.brass, Out);

            Item temp = new Item(1302, 1300, 31, "Apple", "A tasty red apple. Possibly Braeburn.", 'F', 0.3d);
            temp.Init(TCODColor.red, Out);

            LightSource p_torch = new LightSource(1300, 1300, 31, 50, 10, "Torch", "A torch.", '!', 1.0d);
            p_torch.Init(TCODColor.orange, Out);

            map.AddItem(testgun);
            map.AddItem(testmag);
            map.AddItem(temp);
            map.AddItem(p_torch);


            //AICreature testai2 = new AICreature(290, 290, 15, "TEST2", "TEST CREATURE PLEASE IGNORE", 'B');
            //testai2.Init(TCODColor.orange, Out, test_faction, new Objects.Action(ActionType.Idle, null, testai, 0.0d), new WalkerAI(rand.Next(0, 10000000)), map);
            //map.AddCreature(testai2);

            //Item test_item = new Item(1299, 1299, map.DropObject(1299, 1299, 35), "Shimmering rock", "A shining polished rock which seems to change color when you look at it.", (char)'*');
            //LightSource test_item = new LightSource(1299, 1299, map.DropObject(1299, 1299, 35), 12, "Shimmering Rock", "A shining polished rock which seems to change color when you look at it.", (char)'*');
            //test_item_guid = test_item.GUID;
            //test_item.Init(TCODColor.red, Out);
            //test_item.Activate();
            //map.AddItem(test_item);

            LightSource test_item2 = new LightSource(1295, 1310, map.DropObject(1299, 1299, 35), 50, 10, "Shimmering Rock", "A shining polished rock which seems to change color when you look at it.", (char)'*', 1.0d);
            test_item2.Init(TCODColor.blue, Out);
            test_item2.Activate();
            map.AddItem(test_item2);

            LightSource test_item3 = new LightSource(1315, 1283, map.DropObject(1299, 1299, 35), 50, 10, "Shimmering Rock", "A shining polished rock which seems to change color when you look at it.", (char)'*', 1.0d);
            test_item3.Init(TCODColor.green, Out);
            test_item3.Activate();
            map.AddItem(test_item3);

            LightSource test_item4 = new LightSource(1315, 1319, map.DropObject(1299, 1299, 35), 50, 10, "Shimmering Rock", "A shining polished rock which seems to change color when you look at it.", (char)'*', 1.0d);
            test_item4.Init(TCODColor.orange, Out);
            test_item4.Activate();
            map.AddItem(test_item4);

            LightSource test_item5 = new LightSource(1291, 1283, map.DropObject(1299, 1299, 35), 50, 10, "Shimmering Rock", "A shining polished rock which seems to change color when you look at it.", (char)'*', 1.0d);
            test_item5.Init(TCODColor.red, Out);
            test_item5.Activate();
            map.AddItem(test_item5);

            LightSource test_item6 = new LightSource(1305, 1300, map.DropObject(1299, 1299, 35), 50, 10, "Shimmering Rock", "A shining polished rock which seems to change color when you look at it.", (char)'*', 1.0d);
            test_item6.Init(TCODColor.pink, Out);
            test_item6.Activate();
            map.AddItem(test_item6);

            LightSource test_item7 = new LightSource(1291, 1293, map.DropObject(1299, 1299, 35), 50, 10, "Shimmering Rock", "A shining polished rock which seems to change color when you look at it.", (char)'*', 1.0d);
            test_item7.Init(TCODColor.violet, Out);
            test_item7.Activate();
            map.AddItem(test_item7);
        }

        public void Save()
        {
            //Save Map
            map.UnloadMap();

            FileStream fstream = new FileStream(System.IO.Path.Combine(ProfilePath, "save.dat"), FileMode.Create);
            StreamWriter swriter = new StreamWriter(fstream);

            swriter.WriteLine("Version=" + currentVersionCode);
            swriter.WriteLine("MapSeed=" + seed.ToString());

            //Serialize player
            byte[] arr;
            string data;
            Faction pfac;

            MemoryStream mstream = new MemoryStream();
            BinaryFormatter serializer = new BinaryFormatter();

            player.Save();
            pfac = player.Faction;
            pfac.Save();

            //Serialize the player faction into the MemoryStream
            serializer.Serialize(mstream, pfac);

            arr = new byte[mstream.Length];

            //Reset and read the stream into a byte array
            mstream.Seek(0, SeekOrigin.Begin);
            mstream.Read(arr, 0, (int)mstream.Length);

            //Convert the hex-encoded byte array extracted from the serialized MemoryStream
            //into an ascii-encoded string (and remove all dashes).
            data = BitConverter.ToString(arr).Replace("-", string.Empty);

            swriter.WriteLine("FactionData=" + data);

            //Reset stream
            mstream.SetLength(0);
            mstream.Seek(0, SeekOrigin.Begin);

            //Serialize the player into the MemoryStream
            serializer.Serialize(mstream, player);

            arr = new byte[mstream.Length];

            //Reset and read the stream into a byte array
            mstream.Seek(0, SeekOrigin.Begin);
            mstream.Read(arr, 0, (int)mstream.Length);

            //Convert the hex-encoded byte array extracted from the serialized MemoryStream
            //into an ascii-encoded string (and remove all dashes).
            data = BitConverter.ToString(arr).Replace("-", string.Empty);

            swriter.WriteLine("PlayerData=" + data);

            //Save map properties
            swriter.WriteLine("CellWidth=" + wm.CELL_WIDTH);
            swriter.WriteLine("CellHeight=" + wm.CELL_HEIGHT);
            swriter.WriteLine("CellDepth=" + wm.CELL_DEPTH);
            swriter.WriteLine("CellsX=" + wm.CELLS_X);
            swriter.WriteLine("CellsY=" + wm.CELLS_Y);
            swriter.WriteLine("CellsZ=" + wm.CELLS_Z);
            swriter.WriteLine("GroundLevel=" + wm.GROUND_LEVEL);

            //Close, Cleanup
            mstream.Close();
            swriter.Close();
            fstream.Close();
        }

        private void RenderLoadingScreen()
        {
            TCODSystem.setFps(5);

            root.setForegroundColor(TCODColor.grey);
            root.setBackgroundColor(TCODColor.grey);
            root.setBackgroundFlag(TCODBackgroundFlag.Set);
            root.printFrame(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT);
            root.setForegroundColor(TCODColor.black);
            root.print(WINDOW_WIDTH / 2 - 9, WINDOW_HEIGHT / 2, "Map is generating...");

            string seed_string = "Seed: " + seed;
            root.print(WINDOW_WIDTH / 2 - (seed_string.Length / 2), WINDOW_HEIGHT / 2 - 1, seed_string);

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
                RenderAll();
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
            //tar_y = player.Y;
            //tar_x = player.X;
            //tar_z = player.Z;
            tar_x = 0;
            tar_y = 0;
            tar_z = 0;

            switch (key.KeyCode)
            {
                //MOVEMENT
                case TCODKeyCode.KeypadEight:
                    tar_y = - 1;
                    return true;
                case TCODKeyCode.KeypadSix:
                    tar_x =  1;
                    return true;
                case TCODKeyCode.KeypadTwo:
                    tar_y = 1;
                    return true;
                case TCODKeyCode.KeypadFour:
                    tar_x = - 1;
                    return true;
                case TCODKeyCode.KeypadNine:
                    tar_y =  - 1;
                    tar_x =  + 1;
                    return true;
                case TCODKeyCode.KeypadThree:
                    tar_y =  + 1;
                    tar_x =  + 1;
                    return true;
                case TCODKeyCode.KeypadOne:
                    tar_y =  + 1;
                    tar_x =  - 1;
                    return true;
                case TCODKeyCode.KeypadSeven:
                    tar_y =  - 1;
                    tar_x =  - 1;
                    return true;
                case TCODKeyCode.KeypadFive:
                    return true;

                //SELECTION
                case TCODKeyCode.KeypadAdd:
                    menu_selection++;
                    return true;
                case TCODKeyCode.KeypadSubtract:
                    menu_selection--;
                    return true;
                case TCODKeyCode.KeypadEnter:
                    lock_cursor = true;
                    return true;

            }

            switch (key.Character)
            {
                case '<':
                    tar_z = player.Z - 1;
                    return true;
                case '>':
                    tar_z = player.Z + 1;
                    return true;

                case 't':
                    player_pickup = true;
                    return true;
                case 'e':
                    if (mdm == MainDisplayMode.Inventory)
                    {
                        player_equip = true;
                        return true;
                    }
                    return false;
                case 'u':
                    if (mdm == MainDisplayMode.Inventory)
                    {
                        player_unequip = true;
                        return true;
                    }
                    return false;
                case 'a':
                    if (mdm == MainDisplayMode.Game)
                    {
                        player_attack_ranged = true;
                    }
                    return true;
            }

            #endregion


            if (key.Character == 'i')
            {
                mdm = MainDisplayMode.Inventory;
                menu_selection = 0;
                return true;
            }

            if (key.Character == 'c')
            {
                Item i = (Item)Select(SelectionMode.Item);
                //Creature c = (Creature)Select(SelectionMode.Creature);
                return false;
            }

            if (key.Character == 'q')
            {
                mdm = MainDisplayMode.Game;
                CancelDialog();
            }

            if (key.KeyCode == TCODKeyCode.F1)
            {
                tar_x = 1300;
                tar_y = 1300;
                tar_z = map.DropObject(1300, 1300, 49);
                return true;
            }


            if (key.KeyCode == TCODKeyCode.F2)
            {
                tar_x = 1300;
                tar_y = 1300;
                tar_z = map.DropObject(1300, 1300, 31);
                return true;
            }

            if (key.KeyCode == TCODKeyCode.F4)
            {
                map.AddItem((Item)player.Body.SeverRandomBodyPart());
            }

            if (key.KeyCode == TCODKeyCode.F5)
            {
                //DisplayDialog("This is a test message.Dabei wird in Deutschland die Maßeinheit cm zugrunde gelegt, während in Amerika und England die Maßeinheit inch (1 inch = 2,54 cm) für die Hemdgrößen von Herren verwendet wird. Zusätzlich wird die Ärmellänge im Handel gegebenenfalls mit Kurzarm oder Langarm angegeben, jedoch kann dabei die genaue Länge je nach Hersteller unterschiedlich ausfallen. Meist haben dann bei den Herrenhemden zwei aufeinander folgende Hemdgrößen (z.B. 39/40) den gleichen Schnitt des Oberkörpers.");
                SortedDictionary<char, string> test = new SortedDictionary<char, string>();
                test.Add('a', "Test item A");
                test.Add('b', "Test item B");
                test.Add('c', "Test item C");
                Out.SendDebugMessage("Response: " + DisplayInputDialog("Choose one test item. Press q to abort.", test));
            }

            if (key.KeyCode == TCODKeyCode.F6)
            {
                DisplayDialog(player.Body.MakeDescription());
            }

            if (key.KeyCode == TCODKeyCode.F9)
            {
                DisplayDialog(map.ComposeLookAt(player.X, player.Y, player.Z));
            }



            if (key.Character == 'l')
            {
                map.TEST_CIE = map.TEST_CIE ? false : true;
            }

            if (key.KeyCode == TCODKeyCode.F11)
            {
                List<LightSource> templ = new List<LightSource>();

                foreach (Item i in map.ItemList.GetValues())
                {
                    if (i.GetType() == typeof(LightSource))
                    {
                        LightSource l = (LightSource)i;
                        //l.SetPosition(l.X, l.Y, l.Z);
                        //l.SetLevel((byte)(l.LightRadius - 1));
                        l.SetRecalculate(true);
                        templ.Add(l);
                    }

                }

                foreach (LightSource ls in templ)
                {
                    map.ItemList.Remove(ls.GUID);
                    map.ItemList.Add(ls);
                }
            }

            if (key.KeyCode == TCODKeyCode.F12)
            {
                GC.Collect();
                return true;
            }


            if (key.KeyCode == TCODKeyCode.Escape)
            {
                endGame = true;
                return true;
            }
            if (key.KeyCode != TCODKeyCode.NoKey)
            {
                TCODConsole.root.print(0, 0, key.Character.ToString());
                TCODConsole.root.print(0, 1, key.KeyCode.ToString());
                return false;
            }

            return false;
        }

        private void playerPickup()
        {
            List<Item> item_list = new List<Item>();
            SortedDictionary<char, string> char_name_dict = new SortedDictionary<char, string>();

            int ch_int = 97;

            item_list = map.ComposeItems(player.X, player.Y, player.Z);
            if (item_list.Count > 0)
            {
                for (int i = 0; i < item_list.Count; i++)
                {
                    char_name_dict.Add((char)ch_int, item_list[i].Name);
                    ch_int++;
                }

                int resp = DisplayInputDialog("Choose which Item to pick up:", char_name_dict);
                if (resp == -1)
                {
                    player_pickup = false;
                    return;
                }

                //player.Take(map.ItemList[item_list[resp].GUID], map);
                player.Take(item_list[resp], map);
            }

            player_pickup = false;
        }

        private void playerDrop()
        {

        }

        private void playerEquip()
        {
            List<Item> item_list = player.Inventory.GetItemList();
            if (item_list[menu_selection].GetType().IsSubclassOf(typeof(EquippableItem)))
            {
                EquippableItem sel_item = (EquippableItem)item_list[menu_selection];
                player.DoEquip(sel_item);
            }
            else
            {
                Out.SendMessage("Can't equip that item!", Message.MESSAGE_HIGHLIGHT);
            }

            player_equip = false;
        }

        private void playerUnequip()
        {
            List<Item> item_list = player.Inventory.GetItemList();
            if (item_list[menu_selection].GetType().IsSubclassOf(typeof(EquippableItem)))
            {

                EquippableItem sel_item = (EquippableItem)item_list[menu_selection];
                player.Unequip(sel_item.GetSlot());
            }
            else
            {
                Out.SendMessage("Can't unequip that item!", Message.MESSAGE_HIGHLIGHT);
            }

            player_unequip = false;
        }

        private void playerAttackRanged()
        {

        }

        public void RenderEffects(bool render = true)
        {
            Random rand = new Random();
            bool render_dialog = (current_dialog == null) ? false : true;

            effects.setBackgroundColor(TCODColor.black);
            effects.setBackgroundFlag(TCODBackgroundFlag.Set);

            effects.clear();
            effects.setKeyColor(TCODColor.black);

            //effects.setBackgroundColor(TCODColor.orange);
            //effects.setForegroundColor(TCODColor.red);
            //for (int i = 0; i < 10; i++)
            //{
            //    effects.print(rand.Next(1, WINDOW_WIDTH-1), rand.Next(!render_dialog ? 1 : DIALOG_HEIGHT, !render_dialog ? MAIN_HEIGHT : MAIN_HEIGHT - DIALOG_HEIGHT-1), "*");
            //}

            if (render_dialog)
                map.RenderParticles(emit, effects, WINDOW_WIDTH, MAIN_HEIGHT - DIALOG_HEIGHT);
            else
                map.RenderParticles(emit, effects, WINDOW_WIDTH, MAIN_HEIGHT);

            particle_count = emit.particles.Count;
            //main.setForegroundColor(TCODColor.white);
            //main.print(WINDOW_WIDTH - 21, 0, "Particle Count: " + particle_count);

            if (render)
            {
                int offset = render_dialog ? DIALOG_HEIGHT : 0;

                TCODConsole.blit(main, 0, 0, WINDOW_WIDTH, MAIN_HEIGHT, root, 0, 0);
                TCODConsole.blit(effects, 0, 0, WINDOW_WIDTH, MAIN_HEIGHT, root, 0, 0, EFFECTS_ALPHA, EFFECTS_ALPHA);

                if (render_dialog)
                {
                    if (current_dialog.GetType() == typeof(Dialog))
                    {
                        Dialog d = (Dialog)current_dialog;
                        d.Render(dialog);
                    }
                    if (current_dialog.GetType() == typeof(InputDialog))
                    {
                        InputDialog d = (InputDialog)current_dialog;
                        d.Render(dialog);
                    }

                    TCODConsole.blit(dialog, 0, 0, WINDOW_WIDTH, DIALOG_HEIGHT, root, 0, 0);
                }

                TCODConsole.flush();
            }
        }

        public void RenderMessages()
        {
            messages.setForegroundColor(TCODColor.darkAzure); //darkOrange
            messages.setBackgroundColor(TCODColor.darkestBlue); //brass
            messages.setBackgroundFlag(TCODBackgroundFlag.Set);
            messages.printFrame(0, 0, (int)Math.Ceiling(WINDOW_WIDTH * (1.0f - STATUS_TO_MESSAGES_RATIO)), STATUS_HEIGHT);

            messages.setBackgroundFlag(TCODBackgroundFlag.Default);
            messages.setForegroundColor(TCODColor.darkerGrey);

            Out.Render(messages); //Print the message log, y'all
        }

        public void RenderDebugInfo(TCODConsole con)
        {
            con.setForegroundColor(TCODColor.white);
            con.print(2, 0, "Turn: " + turn + " | Gameturn: " + gameTurn);
            con.print(2, 1, "Z_LEVEL: " + player.Z);
            string mem_use = "Memory Usage: " + System.Environment.WorkingSet / 1048576 + " MB";
            con.print(WINDOW_WIDTH - (mem_use.Length + 1), 1, mem_use);

            if (map.initialized)
                con.setForegroundColor(TCODColor.green);
            else
                con.setForegroundColor(TCODColor.red);

            con.print(WINDOW_WIDTH - 1, 0, "+");
        }

        public void RenderStatus()
        {
            status.setForegroundColor(TCODColor.darkAzure);
            status.setBackgroundColor(TCODColor.darkestBlue);
            status.setBackgroundFlag(TCODBackgroundFlag.Set);
            status.printFrame(0, 0, (int)(WINDOW_WIDTH * (STATUS_TO_MESSAGES_RATIO)), STATUS_HEIGHT);

            status.setBackgroundFlag(TCODBackgroundFlag.Default);

            status.print(1, 1, "TESTCHAR: Level 1 Warrior");

            status.print(1, 3, "XP: 0/100");

            status.print(1, 5, "STATUS");
            status.print(2, 6, "R ARM: Fine");
            status.print(2, 7, "L ARM: Fine");
            status.print(2, 8, "R LEG: Fine");
            status.print(2, 9, "L LEG: Fine");
            status.print(2, 10, "TORSO: Fine");
            status.print(2, 11, "HEAD : Fine");

            status.print(1, 13, "INVENTORY");
            status.print(2, 14, "RANGED: [insert weapon here]");
            status.print(4, 15, "AMMO: 10/12");
            status.print(4, 16, "MAGS: 4");
            status.print(2, 17, "MELEE : [insert weapon here]");
            status.print(2, 18, "THROWN: [insert weapon here]");
            status.print(4, 19, "AMMO: 12");

            status.print(2, 21, "WEIGHT: " + player.Inventory.GetWeight() + "/" + player.Inventory.GetCapacity());


        }

        public void RenderInventory(TCODConsole con, int con_x, int con_y, int width, int height)
        {
            con.setForegroundColor(TCODColor.darkAzure);
            con.setBackgroundColor(TCODColor.darkestBlue);
            con.setBackgroundFlag(TCODBackgroundFlag.Set);

            con.printFrame(con_x + 1, con_y + 1, width - 2, height - 2);


            con.setBackgroundFlag(TCODBackgroundFlag.Default);

            con.vline(width / 2, con_y + 2 + 5, height - 4 - 5);
            con.hline(con_x + 2, con_y + 1 + 5, width - 4);

            con.print(con_x + 2, con_y + 2, "GRZ64 INTEGRATED BACKPACK INTERFACE V1.14 REV 1984");
            con.print(con_x + 2, con_y + 3, "UNREGISTERED TRIAL VERSION");

            con.print(con_x + 2, con_y + 5, "LOADING INVENTORY DATA...");

            if (player.Inventory.Count > 0)
            {
                int cap = (height - 5) - (con_y + 7);
                int count = player.Inventory.Count;

                if (menu_selection >= count)
                    menu_selection = (menu_selection - count);

                if (menu_selection < 0)
                    menu_selection = count - 1;

                int top = menu_selection > cap ? (menu_selection - cap) : 0;
                int bottom = top + count > cap ? cap : top + count;

                int it = 0;

                List<Item> item_list = player.Inventory.GetItemList();
                Item sel_item = item_list[menu_selection];

                //Item list
                for (int i = top; i < bottom; i++)
                {
                    con.setBackgroundFlag(TCODBackgroundFlag.Default);

                    if (i == menu_selection)
                    {
                        //con.setBackgroundFlag(TCODBackgroundFlag.Set);
                        //con.setBackgroundColor(TCODColor.azure);
                        con.setForegroundColor(TCODColor.azure);
                    }
                    if (i == menu_selection + 1)
                    {
                        con.setForegroundColor(TCODColor.darkAzure);
                    }

                    if (item_list[i].GetType().IsSubclassOf(typeof(EquippableItem)))
                    {
                        EquippableItem ei = (EquippableItem)item_list[i];
                        if (ei.IsEquipped())
                        {
                            con.setBackgroundFlag(TCODBackgroundFlag.Set);
                            con.setBackgroundColor(TCODColor.lightAzure);
                        }
                    }

                    con.print(con_x + 3, con_y + 7 + it, item_list[i].Name);
                    it++;
                }

                con.setBackgroundFlag(TCODBackgroundFlag.Default);

                con.setForegroundColor(TCODColor.darkAzure);
                int left_offset = width / 2 + 1;
                int top_offset = con_y + 7;

                //Item stats
                con.print(left_offset, top_offset + 0, "NAME         : " + sel_item.Name);
                con.print(left_offset, top_offset + 1, "WEIGHT       : " + sel_item.Weight);

                // type specifics
                if (sel_item.GetType() == typeof(Firearm))
                {
                    Firearm f = (Firearm)sel_item;
                    con.print(left_offset, top_offset + 3, "TYPE         : Firearm");
                    con.print(left_offset, top_offset + 5, "CLASS        : " + f.type.ToString());

                    String fm_str = "";

                    foreach (FireMode fm in f.modes)
                    {
                        fm_str += fm.ToString() + " ";
                    }
                    con.print(left_offset, top_offset + 6, "FIREMODES    : " + fm_str);

                    con.print(left_offset, top_offset + 8, "CALIBER      : " + f.caliber.ToString());
                    con.print(left_offset, top_offset + 9, "MAG. CAPACITY: " + f.MagazineCapacity);

                }


                //Item description
                List<String> lines = wrap(sel_item.Description, width / 2 - 2);
                top_offset = con_y + (int)(height * 0.45d);

                for (int j = 0; j < lines.Count; j++)
                {
                    con.print(left_offset, top_offset + j, lines[j]);
                }


                con.hline(left_offset, top_offset - 1, width / 2 - 2);

                //Item actions
                con.hline(width / 2 + 1, height - 4 - 5, width / 2 - 2);
                //con.print(width / 2 + 2, height - 4 - 4, "R/M/T - Equip Rngd/Mlee/Thrwn");
                //con.print(width / 2 + 2, height - 4 - 3, "  E/L - Equip Armor/Lightsrc");
                con.print(width / 2 + 2, height - 4 - 4, "    E - Equip");
                con.print(width / 2 + 2, height - 4 - 3, "    U - Unequip");
                con.print(width / 2 + 2, height - 4 - 2, "    D - Drop Item");
                con.print(width / 2 + 2, height - 4 - 1, "    Q - Quit");
                con.print(width / 2 + 2, height - 4 + 0, "  +,- - Select");
                con.print(width / 2 + 2, height - 4 + 1, "    C - Consume");
                con.print(width / 2 + 2, height - 4 + 2, "    U - Use");
            }
        }

        private List<String> wrap(String str, int maxchars)
        {
            string temp;
            List<String> lines = new List<string>();

            if (str == null || str == "")
                return null;

            //Oh god the horror that is this function
            //
            //Further down, lines are printed from latest to newest (added to "lines")
            //so in order to display multiline messages correctly, the last of the multiple
            //lines must be added to lines first and the first last. This is done via 
            //a temporary array which is filled from highest to lowest and then added to lines.
            int templines = (int)Math.Ceiling((double)str.Length / (double)maxchars);
            string[] temparr = new string[templines];
            int k = templines - 1;

            temp = str;
            while (temp.Length > maxchars)
            {
                temparr[k] = temp.Substring(0, maxchars);
                temp = temp.Remove(0, maxchars);
                k--;
            }
            temparr[k] = temp;

            foreach (String s in temparr)
            {
                lines.Add(s);
            }

            lines.Reverse();    //WHY THE FU** DID I NOT KNOW THIS FUNCTION BEFORE 
            //I WROTE THIS HORRIFYING MESSAGEHANDLER RENDER FUNCTION

            return lines;
        }

        public void RenderAll()
        {
            bool render_dialog = (current_dialog == null) ? false : true;
            int main_y = render_dialog ? DIALOG_HEIGHT + 1 : 1;
            int main_height = render_dialog ? MAIN_HEIGHT - DIALOG_HEIGHT - 2 : MAIN_HEIGHT - 2;

            main.setBackgroundColor(TCODColor.black);
            main.clear();

            main.setForegroundColor(TCODColor.darkGreen);
            main.printFrame(0, !render_dialog ? 0 : DIALOG_HEIGHT, WINDOW_WIDTH, !render_dialog ? MAIN_HEIGHT : MAIN_HEIGHT - DIALOG_HEIGHT);

            switch (mdm)
            {
                case MainDisplayMode.Game:
                case MainDisplayMode.Cursor:
                    map.Render(main, 1, main_y, WINDOW_WIDTH - 2, main_height);
                    break;
                case MainDisplayMode.Inventory:
                    RenderInventory(main, 1, main_y, WINDOW_WIDTH / 2 - 1, main_height);
                    map.Render(main, WINDOW_WIDTH / 2, main_y, WINDOW_WIDTH / 2 - 1, main_height);
                    break;
            }

            if (mdm == MainDisplayMode.Cursor)
            {
                main.setForegroundColor(TCODColor.azure);
                main.print(cur_rel_x, cur_rel_y, "X");
            }

            if (render_dialog)
            {
                //map.Render(main, 1, DIALOG_HEIGHT + 1, WINDOW_WIDTH - 2, MAIN_HEIGHT - DIALOG_HEIGHT - 2);

                if (current_dialog.GetType() == typeof(Dialog))
                {
                    Dialog d = (Dialog)current_dialog;
                    d.Render(dialog);
                }
                if (current_dialog.GetType() == typeof(InputDialog))
                {
                    InputDialog d = (InputDialog)current_dialog;
                    d.Render(dialog);
                }
            }
            else
            {
                RenderDebugInfo(main);
            }
            TCODConsole.blit(main, 0, 0, WINDOW_WIDTH, MAIN_HEIGHT, root, 0, 0);

            RenderEffects(false);
            TCODConsole.blit(effects, 0, 0, WINDOW_WIDTH, MAIN_HEIGHT, root, 0, 0, EFFECTS_ALPHA, EFFECTS_ALPHA);

            RenderMessages();
            TCODConsole.blit(messages, 0, 0, (int)Math.Ceiling(WINDOW_WIDTH * (1.0f - STATUS_TO_MESSAGES_RATIO)), STATUS_HEIGHT, root, (int)(WINDOW_WIDTH * STATUS_TO_MESSAGES_RATIO), MAIN_HEIGHT);

            RenderStatus();
            TCODConsole.blit(status, 0, 0, (int)(WINDOW_WIDTH * (STATUS_TO_MESSAGES_RATIO)), STATUS_HEIGHT, root, 0, MAIN_HEIGHT);

            if (render_dialog)
                TCODConsole.blit(dialog, 0, 0, WINDOW_WIDTH, DIALOG_HEIGHT, root, 0, 0);

            TCODConsole.flush();

        }
    }
}

