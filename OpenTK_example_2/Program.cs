using System;

namespace OpenTK_example_2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpenTK window");

            using (AppWindow game = new AppWindow(400, 300, "OpenTK 3D mesh"))
            {
                game.Run();
            }
        }
    }
}
