# Artifact Detector

This is a standalone program written in C# for Microsoft Windows with .Net Framework ^4.8.

## Goal

The Artifact Detector detects the visibility of artifacts such as those of the ITS.APE-framework.

## Installation

After installing the dependencies, you can compile the Artifact Detector.

### Dependencies

Install the developer pack of the .NET Framework in version 4.8.
Then, in Microsoft Visual Studio (version 2017 or later), open the solution and install the NuGet packages using the wizard.

### Installation

Compile the solution in MS VS Studio using *Release|x64* configuration. Then run the installer.

## Usage

The Artifact Detector runs as a windows service, the detection can be triggered using a service contract. See the IDetectorService interface for reference.