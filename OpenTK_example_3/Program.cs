using System;

namespace OpenTK_example_3
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpenTK window");

            using (DrawTexture game = new DrawTexture(400, 300, "OpenTK texture"))
            {
                game.Run();
            }
        }
    }
}