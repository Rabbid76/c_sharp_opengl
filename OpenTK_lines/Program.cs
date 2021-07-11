using System;

namespace OpenTK_lines
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpenTK window");

            using (Lines2D lines = new Lines2D(400, 300, "OpenTK"))
            {
                lines.Run();
            }
        }
    }
}
