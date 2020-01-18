using ArbitraryArtifactDetector.Detector;
using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.Parser.Model;
using System;

namespace ArbitraryArtifactDetector.Parser
{
    class DetectorParser
    {
        public static IDetector FromRawRecipe(RawRecipe rawRecipe, Setup setup)
        {
            if (rawRecipe.Detectors.Count < 1)
            {
                throw new ArgumentException("No detectors given for this artifact.");
            }
            else if (rawRecipe.Detectors.Count == 1)
            {
                return FromRawDetectorEntry(rawRecipe.Detectors[0], setup);
            }

            ICompoundDetector detector = new CompoundDetector(setup);
            foreach (RawDetectorEntry rawDetectorEntry in rawRecipe.Detectors)
            {
                detector.AddDetector(FromRawDetectorEntry(rawDetectorEntry, setup));
            }

            return detector;
        }

        private static IDetector FromRawDetectorEntry(RawDetectorEntry rawDetectorEntry, Setup setup)
        {
            string detectorNamespace = typeof(IDetector).Namespace;
            Type detectorType = Type.GetType(detectorNamespace + "." + rawDetectorEntry.DetectorClassName, true, false);
            IDetector detectorInstance = (IDetector) Activator.CreateInstance(detectorType, setup);
            detectorInstance.SetPreConditions(DetectorConditionParser<ArtifactRuntimeInformation>.ParseConditionString(rawDetectorEntry.PreConditions));
            detectorInstance.SetTargetConditions(DetectorConditionParser<DetectorResponse>.ParseConditionString(rawDetectorEntry.Goals));

            return detectorInstance;
        }
    }
}
