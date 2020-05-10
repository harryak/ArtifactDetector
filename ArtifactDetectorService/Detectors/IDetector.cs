using ItsApe.ArtifactDetector.DetectorConditions;
using ItsApe.ArtifactDetector.Models;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Interface for various detectors.
    /// </summary>
    internal interface IDetector
    {
        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="MatchConditions">Condition to determine whether the detector's output yields a match.</param>
        /// <param name="sessionId">ID of the session to detect in (if appliccable).</param>
        DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, IDetectorCondition<ArtifactRuntimeInformation> MatchConditions, int sessionId);

        /// <summary>
        /// Retrieve the conditions previously set.
        /// </summary>
        /// <returns></returns>
        IDetectorCondition<ArtifactRuntimeInformation> GetPreConditions();

        /// <summary>
        /// Retrieve the conditions previously set.
        /// </summary>
        /// <returns></returns>
        IDetectorCondition<DetectorResponse> GetTargetConditions();

        /// <summary>
        /// Tells whether this detector has preconditions.
        /// </summary>
        /// <returns>True if it does.</returns>
        bool HasPreConditions();

        /// <summary>
        /// Tells whether this detector has preconditions.
        /// </summary>
        /// <returns>True if it does.</returns>
        bool HasTargetConditions();

        /// <summary>
        /// Checks whether the current setup and previous response match the conditions for execution of this detector.
        /// </summary>
        /// <param name="runtimeInformation">Response from other detectors run before.</param>
        /// <returns>True if the conditions are met.</returns>
        bool PreConditionsMatch(ref ArtifactRuntimeInformation runtimeInformation);

        /// <summary>
        /// Set conditions to check before this detector may be called.
        /// </summary>
        /// <param name="conditions">A set of conditions.</param>
        void SetPreConditions(IDetectorCondition<ArtifactRuntimeInformation> conditions);

        /// <summary>
        /// Set conditions to change the target for the response.
        /// </summary>
        /// <param name="conditions">A set of conditions.</param>
        void SetTargetConditions(IDetectorCondition<DetectorResponse> conditions);

        /// <summary>
        /// Checks whether the response matches the conditions for evaluating to "artifact found".
        /// </summary>
        /// <param name="previousResponse">Response from this detector.</param>
        /// <returns>True if the conditions are met.</returns>
        bool TargetConditionsMatch(ref DetectorResponse response);
    }
}
