# Artifact Detector

This is a standalone program written in C# for Microsoft Windows with .Net Framework ^4.7.2.

## Goal

The Artifact Detector recognizes the visual information of ITS.APE artifact types in screenshots of a Windows desktop environment.

## Installation

Compile the solution using *Release|x86* configuration. Then browse to the output directory and run program or run installer.

## Usage

The Artifact Detector runs as a standalone program. Executing it without any parameters gives us its help message:
```Batchfile
> Artifact-Detector.exe
Usage: Artifact-Detector.exe [OPTIONS]+
Takes the supplied screenshot and looks in it for the artifact specified.

Options:
  -h, --help                 Show this message and exit.
  -s, --screenshot=VALUE     The path to the screenshot to search in (required).
  -a, --artifact=VALUE       Name of the artifact to look for (required).
  -c, --cache                Cache the artifact types.
  -f, --filepath=VALUE       Path to the working directory (default is current directory).
  -d, --detector=VALUE       Detector to use (default: orb). [akaze, brisk, kaze, orb]
  -e, --evaluate             Include stopwatch.
```

The parameters should be explained if one knows the idea of this program. Those who don't can find an extra explaination here:
Parameter | Explaination
----------|-------------
-s | This is an absolute or relative path for an image file.
-a | Using the itsape/recipes repository, this is the full name of an artifact type, e.g. 07_Paypal-Einfach
-f | This is an absolute or relative path for a directory, the recipes must be stored in this directory.
-d | Expert option regarding feature detection algorithm. Hint: Orb is fastest.
-c | Read from and compile data to a file (in the working directory). If artifact type is not found in this cache, it will be read anew.

### Return value

There are three possible return values:

Value | Meaning
------|--------
0 | The program exited successfully and found a match.
1 | The program exited successfully and found *no* match
-1 | The program had an error while execution

The inversion of the return value logic is due to Windows understanding of return values, where `0` means *successful run*.

When calling the programm from the Windows Cmd, one gets the return value via the variable `errorlevel` like this:
```Batchfile
echo %errorlevel%
```

Calling it from another C#-code can be done like this:
```C#
Process P = Process.Start(sPhysicalFilePath, Param);
P.WaitForExit();
int result = P.ExitCode;
```
