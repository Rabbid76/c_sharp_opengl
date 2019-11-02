using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTK_orbit
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpeTK window");

            using (Orbit app = new Orbit(800, 600, "OpenTK Orbit"))
            {
                app.Run(60.0);
            }
        }
    }
}
