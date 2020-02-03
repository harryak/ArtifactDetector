using ArbitraryArtifactDetector.DebugUtilities;
using ArbitraryArtifactDetector.DetectorConditions;
using ArbitraryArtifactDetector.Models;
using ArbitraryArtifactDetector.Utilities;

namespace ArbitraryArtifactDetector.Detectors
{
    /// <summary>
    /// A base class to provide common functions for all detectors.
    /// </summary>
    internal abstract class BaseDetector : Debuggable, IDetector
    {
        /// <summary>
        /// Provide the setup, please
        /// </summary>
        protected BaseDetector()
        {
            Setup = Setup.GetInstance();
        }

        /// <summary>
        /// Information about the artifact instance currently detecting.
        /// </summary>
        protected ArtifactRuntimeInformation PersistentRuntimeInformation { get; set; }

        /// <summary>
        /// Conditions that have to be fulfilled before this detector should be run.
        /// </summary>
        protected IDetectorCondition<ArtifactRuntimeInformation> PreConditions { get; set; }

        /// <summary>
        /// Setup of this application.
        /// </summary>
        protected Setup Setup { get; }

        /// <summary>
        /// Conditions that have to be fulfilled to yield "match" after calling FindArtifact.
        /// </summary>
        protected IDetectorCondition<DetectorResponse> TargetConditions { get; set; }

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="previousResponse">Optional: Response from another detector run before.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public abstract DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null);

        /// <summary>
        /// Tells whether this detector has preconditions.
        /// </summary>
        /// <returns>True if it does.</returns>
        public bool HasPreConditions()
        {
            // Either the variable is "just not null" or if it is a set it is not empty.
            return PreConditions != null
                && (PreConditions.GetType() != typeof(DetectorConditionSet<ArtifactRuntimeInformation>) || ((DetectorConditionSet<ArtifactRuntimeInformation>) PreConditions).NotEmpty());
        }

        /// <summary>
        /// Tells whether this detector has target conditions.
        /// </summary>
        /// <returns>True if it does.</returns>
        public bool HasTargetConditions()
        {
            // Either the variable is "just not null" or if it is a set it is not empty.
            return TargetConditions != null
                && (TargetConditions.GetType() != typeof(DetectorConditionSet<DetectorResponse>) || ((DetectorConditionSet<DetectorResponse>) TargetConditions).NotEmpty());
        }

        /// <summary>
        /// Checks whether the current setup and previous response match the conditions for execution of this detector.
        /// </summary>
        /// <param name="runtimeInformation">Information from other detectors run before.</param>
        /// <returns>True if the conditions are met.</returns>
        public bool PreConditionsMatch(ArtifactRuntimeInformation runtimeInformation)
        {
            return PreConditions.ObjectMatchesConditions(runtimeInformation);
        }

        /// <summary>
        /// Explicit setter function for the interface.
        /// </summary>
        /// <param name="conditions">Value to set.</param>
        public void SetPreConditions(IDetectorCondition<ArtifactRuntimeInformation> conditions)
        {
            PreConditions = conditions;
        }

        /// <summary>
        /// Explicit setter function for the interface.
        /// </summary>
        /// <param name="conditions">Value to set.</param>
        public void SetTargetConditions(IDetectorCondition<DetectorResponse> conditions)
        {
            TargetConditions = conditions;
        }

        /// <summary>
        /// Checks whether the response matches the conditions for evaluating to "artifact found".
        /// </summary>
        /// <param name="previousResponse">Response from this detector.</param>
        /// <returns>True if the conditions are met.</returns>
        public bool TargetConditionsMatch(DetectorResponse response)
        {
            return TargetConditions.ObjectMatchesConditions(response);
        }
    }
}