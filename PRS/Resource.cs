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

        #endregion

        #region Fields and Properties

        public int id;
        public double s, x;
        public string name;
        public Params buzenParams, simParams;
        public int cumulatedJobs = 0;

        public int putEvents = 0;

        public static Random rand = new Random();

        public double mi { get { return 1 / s; } }

        static StreamWriter sw = new StreamWriter("..//..//log.txt");

        #endregion

        #region Simulation Fields

        public int arrivals = 0, busy = 0, completed = 0;
        public int inBuff = 0;
        public bool working = false;
        public int currJobTime = 0;

        public SMOSystem mySystem;

        #endregion

        #region Simulation Methods

        public void Update()
        {
            cumulatedJobs += (working) ? inBuff + 1 : inBuff;
            if (working)
            {
                busy++;
                currJobTime++;
                if (currJobTime >= s)
                    sendJob();
            }
            else
            {
                if (inBuff > 0)
                {
                    working = true;
                    currJobTime = 0;
                    inBuff--;
                }
            }
        }

        public void sendJob()
        {
            completed++;
            if (inBuff > 0)
            {
                inBuff--;
                currJobTime = 0;
            }
            else
                working = false;

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

            sw.WriteLine("T = " + mySystem.time + " From " + this.name + " to " + mySystem.resources[i].name);

            mySystem.resources[i].putJob();
        }

        public void putJob()
        {
            putEvents++;
        }

        public void clearEvents()
        {
            inBuff += putEvents;
            putEvents = 0;
        }

        public void initializeSimulation()
        {
            simParams.U = simParams.X = simParams.J = simParams.R = 0.0;
            arrivals = busy = completed = 0;
            inBuff = 0;
            working = false;
            currJobTime = 0;
            cumulatedJobs = 0;
            putEvents = 0;
        }

        public void finishSimulation()
        {
            simParams.U = ((double)busy) / mySystem.time;
            simParams.X = ((double)simParams.U) / s;
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
                   "J: " + buzenParams.J + "\n" +
                   "R: " + buzenParams.R + "\n";
        }

        public string SimParams_toString()
        {
            return name + ": " +
                   "\nU = " + simParams.U +
                   "\nX = " + simParams.X +
                   "\nJ = " + simParams.J +
                   "\nR = " + simParams.R;
        }

        #endregion
    }
}
