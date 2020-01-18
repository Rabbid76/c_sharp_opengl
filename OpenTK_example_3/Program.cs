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

            using (DrawTexture window = new DrawTexture(400, 300, "OpenTK texture"))
            {
                window.Run(60.0);
            }
        }
    }
}