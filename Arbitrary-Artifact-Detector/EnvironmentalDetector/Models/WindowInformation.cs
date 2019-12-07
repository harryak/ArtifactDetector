﻿using System;
using System.IO;
using System.Windows.Forms;

namespace ArbitraryArtifactDetector.EnvironmentalDetector.Models
{
    class WindowInformation
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
