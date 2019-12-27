using ArbitraryArtifactDetector.Models;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// A detector made up from several detectors.
    /// </summary>
    class CompoundDetector : ICompoundDetector
    {
        /// <summary>
        /// List of detector configurations.
        /// </summary>
        private IDictionary<int, DetectorConfig> DetectorList { get; set; } = new SortedDictionary<int, DetectorConfig>();

        public int AddDetector(IDetector detector, int priority = -1, int requiredCertainty = 0)
        {
            if (priority < 0)
            {
                priority = DetectorList.Count * 10;
            }

            DetectorList.Add(priority, new DetectorConfig(detector, requiredCertainty));

            return priority;
        }

        public int AddDetector(DetectorConfig detectorConfig, int priority = -1)
        {
            if (priority < 0)
            {
                priority = DetectorList.Count * 10;
            }

            DetectorList.Add(priority, detectorConfig);

            return priority;
        }

        public string DetectorChain()
        {
            string output = "";
            
            foreach (KeyValuePair<int, DetectorConfig> entry in DetectorList)
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

            foreach (KeyValuePair<int, DetectorConfig> entry in DetectorList)
            {
                var nextDetector = entry.Value;
                // Check if previous certainty meets required level.
                if (response != null && nextDetector.HasRequirements() && entry.Value.IsToCall(response))
                    break;

                // Get the new chain element's response.
                response = entry.Value.Detector.FindArtifact(setup);

                // If there is an artifact or there is none with 100 certainty, break.
                if (response.ArtifactPresent || response.Certainty > 99)
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
