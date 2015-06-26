using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PRS
{
    class Resource
    {
        #region Types

        public struct Params
        {
            public double U, X, J, R;
        }

        public class Job
        {
            public int startTime;
            public int runTime;
            public int id;
            public static int idgen = 0;
            public int cycles = -1;

            public Job()
            {
                id = idgen++;
            }
        }

        #endregion

        #region Fields and Properties

        public int id;
        public double s, x;
        public string name;
        public Params buzenParams, simParams;
        public int cumulatedJobs = 0;
        public LinkedList<Job> buffer = new LinkedList<Job>();

        public static Random rand = new Random();

        public double mi { get { return 1 / s; } }

        static StreamWriter sw = new StreamWriter("..//..//log.txt");

        #endregion

        #region Simulation Fields

        public int arrivals = 0, busy = 0, completed = 0;
        public int currJobTime = 0;

        public Job currentJob = null;

        public SMOSystem mySystem;

        #endregion

        #region Simulation Methods

        public void startJob()
        {
            if (currentJob != null)
                finishJob();

            currentJob = buffer.First.Value;
            buffer.RemoveFirst();
            currJobTime = 0;
            if (this.id < mySystem.num_cpu)
                currentJob.cycles++;


            if (buffer.Count == 0 || mySystem.time + currentJob.runTime != buffer.First().startTime)
            {
                mySystem.events.Add(mySystem.time + currentJob.runTime, new SMOSystem.Event(this, mySystem.time + currentJob.runTime, name + " finish job", false));
                //sw.WriteLine("T = " + (mySystem.time + currentJob.runTime) + " job " + currentJob.id + " finish on " + name);
                //sw.Flush();
            }
        }

        public void finishJob()
        {
            cumulatedJobs += (buffer.Count + 1) * currentJob.runTime;
            busy += currentJob.runTime;
            sendJob(currentJob);
        }

        public void sendJob(Job job)
        {
            completed++;
            currentJob = null;

            double r = rand.NextDouble();
            double bottom = 0;
            double upper;

            int i;
            for (i = 0; i < mySystem.NumResources; ++i)
            {
                upper = bottom + mySystem.adjMat[id, i];
                if (r >= bottom && r <= upper)
                    break;
                bottom = upper;
            }

            //sw.WriteLine("T = " + mySystem.time + " Job " + job.id + " From " + this.name + " to " + mySystem.resources[i].name);

            mySystem.resources[i].putJob(job);
        }

        public void putJob(Job job)
        {
            job.runTime = (int)s;
            //job.runTime = (int)(-Math.Log(rand.NextDouble() * s));

            job.startTime = mySystem.time;
            if (buffer.Count != 0)
                job.startTime = buffer.Last.Value.startTime + buffer.Last.Value.runTime;
            else if (currentJob != null)
                job.startTime = currentJob.startTime + currentJob.runTime;

            buffer.AddLast(job);
            mySystem.events.Add(job.startTime, new SMOSystem.Event(this, job.startTime, name + " starts job", true));
            //sw.WriteLine("T = " + mySystem.time + " job " + job.id + " starts on " + name);

        }


        public void initializeSimulation()
        {
            simParams.U = simParams.X = simParams.J = simParams.R = 0.0;
            arrivals = busy = completed = 0;
            buffer.Clear();
            currentJob = null;
            currJobTime = 0;
            cumulatedJobs = 0;
        }

        public void finishSimulation()
        {
            simParams.U = ((double)busy) / mySystem.time;
            simParams.X = ((double)completed) / mySystem.time;
            simParams.J = ((double)cumulatedJobs) / mySystem.time;
            simParams.R = ((double)simParams.J) / simParams.X;

            sw.Flush();
        }

        #endregion

        #region Initialization

        public Resource(string name, double s, int id, SMOSystem mySystem)
        {
            this.name = name;
            this.s = s;
            this.id = id;
            this.mySystem = mySystem;
        }

        #endregion

        #region Other

        public string BuzenParams_toString()
        {
            return name + ": \n" +
                   "U: " + buzenParams.U + "\n" +
                   "X: " + buzenParams.X + "\n" +
                   "J: " + buzenParams.J + "\n";
                   //"R: " + buzenParams.R + "\n";
        }

        public string SimParams_toString()
        {
            return name + ": " +
                   "\nU = " + simParams.U +
                   "\nX = " + simParams.X +
                   "\nJ = " + simParams.J;// +
                   //"\nR = " + simParams.R;
        }

        #endregion
    }
}
