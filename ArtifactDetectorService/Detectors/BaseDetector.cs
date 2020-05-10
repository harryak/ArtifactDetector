using ItsApe.ArtifactDetector.DebugUtilities;
using ItsApe.ArtifactDetector.DetectorConditions;
using ItsApe.ArtifactDetector.Models;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// A base class to provide common functions for all detectors.
    /// </summary>
    internal abstract class BaseDetector : HasLogger, IDetector
    {
        /// <summary>
        /// Conditions that have to be fulfilled before this detector should be run.
        /// </summary>
        protected IDetectorCondition<ArtifactRuntimeInformation> PreConditions { get; set; }

        /// <summary>
        /// Conditions that have to be fulfilled to yield "match" after calling FindArtifact.
        /// </summary>
        protected IDetectorCondition<DetectorResponse> TargetConditions { get; set; }

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="MatchConditions">Condition to determine whether the detector's output yields a match.</param>
        /// <param name="sessionId">ID of the session to detect in (if appliccable).</param>
        public abstract DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, IDetectorCondition<ArtifactRuntimeInformation> MatchConditions, int sessionId);

        /// <summary>
        /// Wrapper for the pre-conditions.
        /// </summary>
        /// <returns>The currently set pre-conditions of the detector.</returns>
        public IDetectorCondition<ArtifactRuntimeInformation> GetPreConditions()
        {
            return PreConditions;
        }

        /// <summary>
        /// Wrapper for the target conditions.
        /// </summary>
        /// <returns>The currently set target conditions of the detector.</returns>
        public IDetectorCondition<DetectorResponse> GetTargetConditions()
        {
            return TargetConditions;
        }

        /// <summary>
        /// Tells whether this detector has preconditions.
        /// </summary>
        /// <returns>True if it does.</returns>
        public bool HasPreConditions()
        {
            // Either the variable is "just not null" or if it is a set it is not empty.
            return PreConditions != null
                && (PreConditions.GetType() != typeof(DetectorConditionSet<ArtifactRuntimeInformation>) || ((DetectorConditionSet<ArtifactRuntimeInformation>)PreConditions).NotEmpty());
        }

        /// <summary>
        /// Tells whether this detector has target conditions.
        /// </summary>
        /// <returns>True if it does.</returns>
        public bool HasTargetConditions()
        {
            // Either the variable is "just not null" or if it is a set it is not empty.
            return TargetConditions != null
                && (TargetConditions.GetType() != typeof(DetectorConditionSet<DetectorResponse>) || ((DetectorConditionSet<DetectorResponse>)TargetConditions).NotEmpty());
        }

        /// <summary>
        /// Checks whether the current setup and previous response match the conditions for execution of this detector.
        /// </summary>
        /// <param name="runtimeInformation">Information from other detectors run before.</param>
        /// <returns>True if the conditions are met.</returns>
        public bool PreConditionsMatch(ref ArtifactRuntimeInformation runtimeInformation)
        {
            return PreConditions.ObjectMatchesConditions(ref runtimeInformation);
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
        public bool TargetConditionsMatch(ref DetectorResponse response)
        {
            return TargetConditions.ObjectMatchesConditions(ref response);
        }
    }
}
