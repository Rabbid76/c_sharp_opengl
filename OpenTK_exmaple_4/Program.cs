using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTK_exmaple_4
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create OpeTK window");

            using (ComputeBarnsleyFern window = new ComputeBarnsleyFern(400, 400, "OpenTK compute shader - Barnsley fern"))
            {
                window.Run(60.0);
            }
        }
    }
}
