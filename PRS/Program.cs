using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRS
{
    class Program
    {
        static void Main(string[] args)
        {
            SMOSystem smo = new SMOSystem("..//..//inputFile.txt");
            Console.Write("Simulation time in minutes ");
            int mins = Int32.Parse(Console.ReadLine());

            for (int i = 2; i <= 5; ++i)
                for (int k = 5; k <= 15; k += 5)
                {
                    smo.init(i, k);
                    smo.Analyse();
                    smo.simulate(mins * 60 * 1000);
                }
        }
    }
}