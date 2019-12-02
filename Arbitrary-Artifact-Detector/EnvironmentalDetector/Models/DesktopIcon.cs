

using System.Drawing;
/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/
namespace ArbitraryArtifactDetector.EnvironmentalDetector.Models
{
    public class DesktopIcon
    {
        internal DesktopIcon(string name, Point location)
        {
            Location = location;
            Name = name;
        }

        internal Point Location { get; set; }

        public string Name { get; set;  }

        public override string ToString()
        {
            return Name + " at " + Location.X + "," + Location.Y;
        }
    }
}
