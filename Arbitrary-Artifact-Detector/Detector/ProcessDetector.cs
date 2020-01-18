using ArbitraryArtifactDetector.Model;
using System.Diagnostics;

namespace ArbitraryArtifactDetector.Detector
{
    internal class ProcessDetector : BaseDetector, IDetector
    {
        public ProcessDetector(Setup setup) : base(setup)
        {
        }

        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, Setup setup, DetectorResponse previousResponse = null)
        {
            Process[] processes = Process.GetProcessesByName("ArtifactDetector");

            if (processes.Length < 1) return new DetectorResponse() { ArtifactPresent = false, Certainty = 100 };

            foreach(Process currentProcess in processes)
            {
                runtimeInformation.WindowHandle = currentProcess.Handle;
            }

            return new DetectorResponse() { ArtifactPresent = true, Certainty = 10 };
        }
    }
}