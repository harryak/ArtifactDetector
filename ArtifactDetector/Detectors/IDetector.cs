﻿using ItsApe.ArtifactDetector.DetectorConditions;
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
        /// <param name="previousResponse">Optional: Response from another detector run before.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null);

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
        bool PreConditionsMatch(ArtifactRuntimeInformation runtimeInformation);

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
        bool TargetConditionsMatch(DetectorResponse response);
    }
}