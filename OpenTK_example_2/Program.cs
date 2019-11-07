using System;
using System.Collections.Generic;
using System.Text;

namespace OpenTK_example_2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpeTK window");

            using (AppWindow game = new AppWindow(800, 600, "OpenTK"))
            {
                //Run takes a double, which is how many frames per second it should strive to reach.
                //You can leave that out and it'll just update as fast as the hardware will allow it.
                game.Run(60.0);
            }
        }
    }
}
