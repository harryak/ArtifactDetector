namespace ArbitraryArtifactDetector.Detectors
{
    class DetectorConverter
    {
        /*public IDetector GetDetector()
        {
            // Only instantiate detector once.
            if (detectorConfiguration.Detector != null)
            {
                return detectorConfiguration.Detector;
            }

            Type detectorType = typeof(IDetector);
            string Namespace = detectorType.Namespace;

            throw new NotImplementedException();
        }

        public IDetector GetDetector(List<DetectorConfiguration> detectorConfigurations)
        {
            if (detectorConfigurations.Count < 1)
            {
                throw new ArgumentException("Could not get detectors for empty list of configurations.");
            }

            if (detectorConfigurations.Count == 1)
            {
                return GetDetector(detectorConfigurations[0]);
            }

            var compoundDetector = new CompoundDetector();
            foreach(DetectorConfiguration detectorConfiguration in detectorConfigurations)
            {
                compoundDetector.AddDetector(detectorConfiguration);
            }

            return compoundDetector;
        }

        public IDetector GetDetector(ArtifactConfiguration artifactConfiguration)
        {
            return GetDetector();
        }*/
    }
}
