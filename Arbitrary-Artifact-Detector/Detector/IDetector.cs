/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

namespace ArbitraryArtifactDetector.Detector
{
    interface IDetector
    {
        bool FindArtifact(Setup setup);
    }
}
