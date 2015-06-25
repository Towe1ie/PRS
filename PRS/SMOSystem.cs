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

        public double[,] adjMat;
        public int time = 0;

        #endregion

        #region Properties

        public int NumResources
        {
            get { return num_cpu + num_sys_disc + num_usr_disc; }
        }

        #endregion

        #region Initialization

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
            generateAdjMat();
        }

        public void generateResources()
        {
            int idGen = 0;
            for (int i = 0; i < num_cpu; ++i)
                resources.Add(new Resource("CPU " + i, s_equivalent[0] / num_cpu, idGen++, this));
            for (int i = num_cpu; i < num_cpu + num_sys_disc; ++i)
                resources.Add(new Resource("System disc " + i, s_equivalent[1] / num_sys_disc, idGen++, this));
            for (int i = num_cpu + num_sys_disc; i < NumResources; ++i)
                resources.Add(new Resource("User disc " + i, s_equivalent[2] / num_usr_disc, idGen++, this));
        }

        void generateAdjMat()
        {
            adjMat = new double[NumResources, NumResources];
            for (int i = 0; i < num_cpu; ++i)
            {
                for (int j = 0; j < num_cpu; ++j)
                {
                    adjMat[i, j] = p_equivalent[0, 0] / num_cpu;
                }

                for (int j = num_cpu; j < num_cpu + num_sys_disc; ++j)
                {
                    adjMat[i, j] = p_equivalent[0, 1] / num_sys_disc;
                }

                for (int j = num_cpu + num_sys_disc; j < num_cpu + num_sys_disc + num_usr_disc; ++j)
                {
                    adjMat[i, j] = p_equivalent[0, 2] / num_usr_disc;
                }
            }

            for (int i = num_cpu; i < num_cpu + num_sys_disc; ++i)
            {
                for (int j = 0; j < num_cpu; ++j)
                {
                    adjMat[i, j] = p_equivalent[1, 0] / num_cpu;
                }

                for (int j = num_cpu; j < num_cpu + num_sys_disc; ++j)
                {
                    adjMat[i, j] = p_equivalent[1, 1] / num_sys_disc;
                }

                for (int j = num_cpu + num_sys_disc; j < num_cpu + num_sys_disc + num_usr_disc; ++j)
                {
                    adjMat[i, j] = p_equivalent[1, 2] / num_usr_disc;
                }
            }

            for (int i = num_cpu + num_sys_disc; i < NumResources; ++i)
            {
                for (int j = 0; j < num_cpu; ++j)
                {
                    adjMat[i, j] = p_equivalent[2, 0] / num_cpu;
                }

                for (int j = num_cpu; j < num_cpu + num_sys_disc; ++j)
                {
                    adjMat[i, j] = p_equivalent[2, 1] / num_sys_disc;
                }

                for (int j = num_cpu + num_sys_disc; j < num_cpu + num_sys_disc + num_usr_disc; ++j)
                {
                    adjMat[i, j] = p_equivalent[2, 2] / num_usr_disc;
                }
            }
        }

        #endregion

        #region Buzen's algorithm

        public void Analyse()
        {
            Gordon_Newell();
            Buzen();
            CalcOutParameters();

            StreamWriter sw = new StreamWriter("../../buzenOut.txt");

            foreach(Resource r in resources)
            {
                sw.WriteLine(r.BuzenParams_toString());
            }

            sw.Close();
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
            for (int i = 1; i <= num_jobs; ++i)
                G[i] = 0;

            foreach(Resource r in resources)
            {
                for (int i = 1; i <= num_jobs; ++i)
                    G[i] = G[i] + r.x * G[i - 1];
            }
        }

        public void CalcOutParameters()
        {
            foreach(Resource r in resources)
            {
                r.buzenParams.U = r.x * G[num_jobs - 1] / G[num_jobs];
                r.buzenParams.X = r.buzenParams.U / r.s;

                r.buzenParams.J = 0;
                for (int i = 1; i <= num_jobs; ++i)
                    r.buzenParams.J += Math.Pow(r.x, i) * G[num_jobs - i] / G[num_jobs];

                r.buzenParams.R = r.buzenParams.J / r.buzenParams.X;
            }
        }

        #endregion

        #region Simulation

        public void simulate(int simulationTime)
        {
            foreach (Resource r in resources)
            {
                r.initializeSimulation();
            }

            for (int i = 0; i < num_jobs; ++i)
                resources[i % NumResources].putJob();

            for (time = 0; time < simulationTime; ++time)
            {
                foreach (Resource r in resources)
                    r.clearEvents();
                foreach (Resource r in resources)
                    r.Update();
            }

            StreamWriter sw = new StreamWriter("..//..//simOut.txt");
            foreach (Resource r in resources)
            {
                r.finishSimulation();
                sw.WriteLine(r.SimParams_toString() + "\n");
            }
            sw.Close();
        }

        #endregion
    }
}