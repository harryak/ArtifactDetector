using ArbitraryArtifactDetector.Models;
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
                output += "{entry.Key} : {entry.Value };";
            }

            return output;
        }

        public int DetectorCount()
        {
            return DetectorList.Count;
        }

        public DetectorResponse FindArtifact(Setup setup)
        {
            DetectorResponse response = null;

            foreach (KeyValuePair<int, IDetector> entry in DetectorList)
            {
                response = entry.Value.FindArtifact(setup);

                if (response.ArtifactFound || (!response.ArtifactLikely && response.Certainty > 99))
                    break;
            }

            return response;
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
