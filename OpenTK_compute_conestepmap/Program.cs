using System;

namespace OpenTK_compute_conestepmap
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("compute cone step map");

            using (AppWindow game = new AppWindow(400, 300, "OpenTK compute cone step map"))
            {
                game.Run();
            }
        }
    }
}
