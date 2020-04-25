using System;

namespace ItsApe.ArtifactDetector.Models
{
    public ref struct RuntimeInformationSpan
    {
        public Span<string> possibleWindowTitles;

        public RuntimeInformationSpan FromArtifactRuntimeInformation(ArtifactRuntimeInformation runtimeInformation)
        {
            possibleWindowTitles = new Span<string>(runtimeInformation.PossibleWindowTitleSubstrings.ToArray());
            return this;
        }
    }
}