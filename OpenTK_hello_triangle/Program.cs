using System;

namespace OpenTK_hello_triangle
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello Triangle!");
            using (HelloTriangle game = new HelloTriangle())
            {
                game.Run();
            }
        }
    }
}
