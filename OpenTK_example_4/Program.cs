using System;

namespace OpenTK_exmaple_4
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpenTK window");

            using (ComputeBarnsleyFern game = new ComputeBarnsleyFern(400, 400, "OpenTK compute shader - Barnsley fern"))
            {
                game.Run();
            }
        }
    }
}
