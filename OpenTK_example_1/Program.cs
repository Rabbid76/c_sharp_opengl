using System;

namespace OpenTK_example_1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpeTK window");

            using (Game game = new Game(400, 300, "OpenTK"))
            {
                game.Run();
            }
        }
    }
}
