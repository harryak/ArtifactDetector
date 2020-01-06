using ArbitraryArtifactDetector.DebugUtilities;
using ArbitraryArtifactDetector.DetectorCondition.Model;
using ArbitraryArtifactDetector.Model;

namespace ArbitraryArtifactDetector.Detectors
{
    /// <summary>
    /// A base class to provide common functions for all detectors.
    /// </summary>
    abstract class BaseDetector : Debuggable, IDetector
    {
        protected DetectorConditionSet PreConditions { get; set; }
        protected DetectorConditionSet TargetConditions { get; set; }

        /// <summary>
        /// Provide the setup, please
        /// </summary>
        protected BaseDetector(Setup setup) : base(setup)
        {

        }

        /// <summary>
        /// Explicit setter function for the interface.
        /// </summary>
        /// <param name="conditions">Value to set.</param>
        public void SetPreConditions(DetectorConditionSet conditions)
        {
            PreConditions = conditions;
        }

        /// <summary>
        /// Explicit setter function for the interface.
        /// </summary>
        /// <param name="conditions">Value to set.</param>
        public void SetTargetConditions(DetectorConditionSet conditions)
        {
            TargetConditions = conditions;
        }

        /// <summary>
        /// Tells whether this detector has preconditions.
        /// </summary>
        /// <returns>True if it does.</returns>
        public bool HasPreConditions()
        {
            return PreConditions != null && PreConditions.NotEmpty();
        }

        /// <summary>
        /// Checks whether the current setup and previous response match the conditions for execution of this detector.
        /// </summary>
        /// <param name="previousResponse">Response from another detector run before.</param>
        /// <returns>True if the conditions are met.</returns>
        public bool PreConditionsMatch(DetectorResponse previousResponse)
        {
            return PreConditions.ResponseMatchesConditions(previousResponse);
        }

        /// <summary>
        /// Checks whether the response matches the conditions for evaluating to "artifact found".
        /// </summary>
        /// <param name="previousResponse">Response from this detector.</param>
        /// <returns>True if the conditions are met.</returns>
        public bool TargetConditionsMatch(DetectorResponse response)
        {
            return PreConditions.ResponseMatchesConditions(response);
        }

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="setup">Setup</param>
        /// <param name="previousResponse">Optional: Response from another detector run before.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public abstract DetectorResponse FindArtifact(ArtifactRuntimeInformation runtimeInformation, Setup setup, DetectorResponse previousResponse = null);
    }
}
