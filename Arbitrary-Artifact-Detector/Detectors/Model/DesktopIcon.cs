﻿using System.Drawing;

namespace ItsApe.ArtifactDetector.Models
{
    class DesktopIcon
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
