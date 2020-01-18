using System;

namespace OpenTK_example_5
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpeTK window");

            using (DrawText window = new DrawText(400, 300, "OpenTK text"))
            {
                window.Run(60.0);
            }
        }
    }
}
