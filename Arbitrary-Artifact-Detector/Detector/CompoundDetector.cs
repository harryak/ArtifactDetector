using ArbitraryArtifactDetector.Model;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// A detector made up from several detectors.
    /// </summary>
    class CompoundDetector : BaseDetector, ICompoundDetector
    {
        /// <summary>
        /// List of detector configurations.
        /// </summary>
        private IDictionary<int, IDetector> DetectorList { get; set; } = new SortedDictionary<int, IDetector>();

        public CompoundDetector(Setup setup) : base(setup)
        {

        }

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

        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, Setup setup, DetectorResponse previousResponse = null)
        {
            DetectorResponse response = null;

            foreach (KeyValuePair<int, IDetector> entry in DetectorList)
            {
                var currentDetector = entry.Value;
                // Check if previous certainty meets required level.
                if (previousResponse != null && currentDetector.HasPreConditions() && entry.Value.PreConditionsMatch(runtimeInformation))
                    break;

                // Get the new chain element's response.
                response = currentDetector.FindArtifact(ref runtimeInformation, setup, previousResponse);

                // If there is an artifact or there is none with 100 certainty, break.
                if (currentDetector.TargetConditionsMatch(response))
                    break;

                // Copy to right variable for next run.
                previousResponse = response;
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
