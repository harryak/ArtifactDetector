/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using System.Drawing;

namespace ArbitraryArtifactDetector.EnvironmentalDetector.Models
{
    public class DesktopIcon
    {
        public DesktopIcon(string name, Point location)
        {
            Location = location;
            Name = name;
        }

        public Point Location { get; }

        public string Name { get;  }

        public override string ToString()
        {
            return Name + " at " + Location.X + "," + Location.Y;
        }
    }
}
