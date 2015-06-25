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
            smo.Analyse();
            smo.simulate(100000);
        }
    }
}
