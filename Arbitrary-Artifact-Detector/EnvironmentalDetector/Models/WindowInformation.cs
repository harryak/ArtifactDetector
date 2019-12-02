/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using System;
using System.IO;
using System.Windows.Forms;

namespace ArbitraryArtifactDetector.EnvironmentalDetector.Models
{
    public class WindowInformation
    {
        public IntPtr Handle = IntPtr.Zero;
        public FileInfo ExecutablePath = new FileInfo(Application.ExecutablePath);
        public string Title = Application.ProductName;
        internal WindowPlacement Placement = new WindowPlacement();

        public override string ToString()
        {
            return ExecutablePath.Name;
        }
    }
}
