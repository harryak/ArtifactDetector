/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Detector
{
    class CompoundDetector : ICompoundDetector
    {
        private IDictionary<int, IDetector> DetectorList { get; set; } = new SortedDictionary<int, IDetector>();

        public int AddDetector(IDetector detector, int priority = -1)
        {
            if (priority < 0)
            {
                priority = DetectorList.Count * 10;
            }

            DetectorList.Add(priority, detector);

            return priority;
        }

        public string DetectorChain()
        {
            string output = "";
            
            foreach (KeyValuePair<int, IDetector> entry in DetectorList)
            {
                output += entry.Key + ": " + entry.Value + ";";
            }

            return output;
        }

        public int DetectorCount()
        {
            return DetectorList.Count;
        }

        public bool FindArtifact(Setup setup)
        {
            bool output = false;

            foreach (KeyValuePair<int, IDetector> entry in DetectorList)
            {
                output |= entry.Value.FindArtifact(setup);

                if (output)
                    break;
            }

            return output;
        }

        public bool PrioritizeDetector(IDetector detector, int oldPriority, int newPriority)
        {
            if (DetectorList.ContainsKey(oldPriority))
            {
                RemoveDetector(detector, oldPriority);
            }

            return AddDetector(detector, newPriority) == newPriority;
        }

        public bool RemoveDetector(IDetector detector, int priority)
        {
            return DetectorList.Remove(priority);
        }
    }
}
