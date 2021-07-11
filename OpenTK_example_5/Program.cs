using System;

namespace OpenTK_example_5
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpenTK window");

            using (DrawText game = new DrawText(400, 300, "OpenTK text"))
            {
                game.Run();
            }
        }
    }
}
