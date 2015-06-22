using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRS
{
    class Resource
    {
        public struct Params
        {
            double U, X, J, T;
        }

        public int id;
        public double s, x;
        public double mi { get { return 1 / s; } }

        public string name;

        public Params buzenParams, simParams;

        public Resource(string name, double s, int id)
        {
            this.name = name;
            this.s = s;
            this.id = id;
        }


    }
}
