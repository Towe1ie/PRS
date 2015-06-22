using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PRS
{
    class SMOSystem
    {
        #region Types

        enum Component { CPU = 0, USR_DISC, SYS_DISC };

        #endregion

        #region Fields

        public List<Resource> resources = new List<Resource>();

        int num_cpu, num_sys_disc, num_usr_disc;
        int num_jobs;
        double[] s_equivalent = new double[3];
        double[,] p_equivalent = new double[3, 3];

        double[] G;

        #endregion

        #region Properties

        public int NumResources
        {
            get { return num_cpu + num_sys_disc + num_sys_disc; }
        }

        #endregion

        private void parseFile(string inputFileName)
        {
            StreamReader sr = new StreamReader(inputFileName);

            // first line num proc, sys disc, usr disc
            string line = sr.ReadLine();
            string[] tokens = line.Split(' ');

            num_cpu = Int32.Parse(tokens[0]);
            num_sys_disc = Int32.Parse(tokens[1]);
            num_usr_disc = Int32.Parse(tokens[2]);
            num_jobs = Int32.Parse(tokens[3]);

            // second line s
            line = sr.ReadLine();
            tokens = line.Split(' ');

            s_equivalent[0] = num_cpu * double.Parse(tokens[0]);
            s_equivalent[1] = num_sys_disc * double.Parse(tokens[1]);
            s_equivalent[2] = num_usr_disc * double.Parse(tokens[2]);

            // next three lines matrix of probabilites between SUBSYSTEMS as whole
            for (int i = 0; i < 3; ++i)
            {
                line = sr.ReadLine();
                tokens = line.Split(' ');

                for (int j = 0; j < 3; ++j)
                    p_equivalent[i, j] = double.Parse(tokens[j]);
            }
        }

        public SMOSystem(string inputFileName)
        {
            parseFile(inputFileName);
            generateResources();
        }

        public void generateResources()
        {
            int idGen = 0;
            for (int i = 0; i < num_cpu; ++i)
                resources.Add(new Resource("CPU " + i, s_equivalent[0] / num_cpu, idGen++));
            for (int i = num_cpu; i < num_cpu + num_sys_disc; ++i)
                resources.Add(new Resource("System disc " + i, s_equivalent[1] / num_sys_disc, idGen++));
            for (int i = num_cpu + num_sys_disc; i < NumResources; ++i)
                resources.Add(new Resource("User disc " + i, s_equivalent[2] / num_usr_disc, idGen++));
        }

        public void Analyse()
        {
            Gordon_Newell();
            Buzen();
        }

        public void Gordon_Newell()
        {
            for (int i = 0; i < num_cpu; ++i)
                resources[i].x = 1.0;
            for (int i = num_cpu; i < num_cpu + num_sys_disc; ++i)
                resources[i].x = p_equivalent[0,1] * resources[0].mi / resources[1].mi;
            for (int i = num_cpu + num_sys_disc; i < NumResources; ++i)
                resources[i].x = (p_equivalent[0,2] * resources[0].mi + p_equivalent[1,2] * resources[1].mi * resources[1].x) / (resources[2].mi);
        }

        public void Buzen()
        {
            G = new double[num_jobs + 1];
            G[0] = 1;
            for (int i = 1; i <= NumResources; ++i)
                G[i] = 0;

            foreach(Resource r in resources)
            {
                for (int i = 1; i <= num_jobs; ++i)
                    G[i] = G[i] + r.x * G[i - 1];
            }
        }
    }
}
