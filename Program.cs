using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShootyShootyRL
{
    class Program
    {
        public static Game game;

        [STAThread]
        static void Main(string[] args)
        {
            game = new Game();
            game.Run();
        }
    }

    class UniversalSpaceTimeException : Exception
    {
    }
}
