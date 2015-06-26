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

        public class Event
        {
            public Resource resource;
            public int time;
            string message;
            public bool start;

            public Event(Resource r, int t, string m, bool start)
            {
                resource = r;
                time = t;
                message = m;
                this.start = start;
            }
        }

        #endregion

        #region Fields

        public List<Resource> resources = new List<Resource>();
        public List<Resource.Job> jobs = new List<Resource.Job>();
        public SortedList<int, Event> events = new SortedList<int, Event>(new DuplicateKeyComparer<int>());

        public int num_cpu, num_sys_disc, num_usr_disc;
        int num_jobs;
        double[] s_equivalent = new double[3];
        double[,] p_equivalent = new double[3, 3];

        double[] G;

        public double[,] adjMat;
        public int time = 0;

        StreamWriter sw = new StreamWriter("../../buzenOut.txt");
        StreamWriter swU = new StreamWriter("../../U.txt");
        StreamWriter swX = new StreamWriter("../../X.txt");
        StreamWriter swJ = new StreamWriter("../../J.txt");
        StreamWriter simSw = new StreamWriter("../../simOut.txt");
        StreamWriter simU = new StreamWriter("../../simU.txt");
        StreamWriter simX = new StreamWriter("../../simX.txt");
        StreamWriter simJ = new StreamWriter("../../simJ.txt");

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

        public void init(int numDisc, int numJobs)
        {
            num_usr_disc = numDisc;
            num_jobs = numJobs;
            generateResources();
            generateAdjMat();
        }

        public void generateResources()
        {
            resources = new List<Resource>();
            int idGen = 0;
            for (int i = 0; i < num_cpu; ++i)
                resources.Add(new Resource("CPU " + i, s_equivalent[0] / num_cpu, idGen++, this));
            for (int i = 0; i < num_sys_disc; ++i)
                resources.Add(new Resource("System disc " + i, s_equivalent[1] / num_sys_disc, idGen++, this));
            for (int i = 0; i < num_usr_disc; ++i)
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

            //StreamWriter sw = new StreamWriter("../../buzenOut_" + num_usr_disc + "_" + num_jobs + ".txt");
            sw.WriteLine("**** Num disc = " + num_usr_disc + ", Num jobs = " + num_jobs + "****");

            swX.WriteLine(resources[0].buzenParams.X);
            swU.WriteLine(resources[0].buzenParams.U);
            swJ.WriteLine(resources[0].buzenParams.J);
            swX.Flush();
            swU.Flush();
            swJ.Flush();

            foreach (Resource r in resources)
                sw.WriteLine(r.BuzenParams_toString());

            sw.WriteLine("T = " + num_jobs / (2 * resources[0].buzenParams.X));
            sw.WriteLine();
            sw.Flush();
            //sw.Close();
        }

        public void Gordon_Newell()
        {
            for (int i = 0; i < num_cpu; ++i)
                resources[i].x = 1.0;
            for (int i = num_cpu; i < num_cpu + num_sys_disc; ++i)
                resources[i].x = num_cpu * (p_equivalent[0, 1] * resources[0].mi / resources[num_cpu].mi) / num_sys_disc;
            for (int i = num_cpu + num_sys_disc; i < NumResources; ++i)
                resources[i].x = num_cpu * (p_equivalent[0, 2] * resources[0].mi + p_equivalent[1, 2] * resources[num_cpu].mi * resources[num_cpu].x) / (resources[num_cpu + num_sys_disc].mi) / num_usr_disc;
        }

        public void Buzen()
        {
            G = new double[num_jobs + 1];
            G[0] = 1;
            for (int i = 1; i <= num_jobs; ++i)
                G[i] = 0;

            foreach (Resource r in resources)
            {
                for (int i = 1; i <= num_jobs; ++i)
                    G[i] = G[i] + r.x * G[i - 1];
            }
        }

        public void CalcOutParameters()
        {
            foreach (Resource r in resources)
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
                r.initializeSimulation();
            time = 0;
            jobs = new List<Resource.Job>();
            events = new SortedList<int, Event>(new DuplicateKeyComparer<int>());
            for (int i = 0; i < num_jobs; ++i)
            {
                Resource.Job job = new Resource.Job();
                jobs.Add(job);
                resources[0].putJob(job);
            }

            while (events.Count != 0 && time < simulationTime)
            {
                int key = events.First().Key;

                while (events.Count != 0 && events.First().Key == key)
                {
                    Event e = events.First().Value;
                    events.RemoveAt(0);
                    int elapsed = e.time - time;
                    time = e.time;

                    if (e.start)
                        e.resource.startJob();
                    else
                        e.resource.finishJob();
                }

            }

            //StreamWriter sw = new StreamWriter("..//..//simOut.txt");

            simSw.WriteLine("**** Num disc = " + num_usr_disc + ", Num jobs = " + num_jobs + "****");
            double T = 0;
            foreach (Resource r in resources)
            {
                r.finishSimulation();
                simSw.WriteLine(r.SimParams_toString() + "\n");
            }

            simX.WriteLine(resources[0].simParams.X);
            simU.WriteLine(resources[0].simParams.U);
            simJ.WriteLine(resources[0].simParams.J);
            simX.Flush();
            simU.Flush();
            simJ.Flush();

            foreach (Resource.Job job in jobs)
            {
                T += time / ((double)job.cycles);
            }

            T /= num_jobs;

            simSw.WriteLine("T = " + T);
            simSw.WriteLine();
            simSw.Flush();
        }

        #endregion
    }
}