using System;
using System.Collections.Generic;
using System.Text;

namespace OpenTK_example_3
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpeTK window");

            using (UseTexture window = new UseTexture(800, 600, "OpenTK texture object"))
            {
                window.Run(60.0);
            }
        }
    }
}