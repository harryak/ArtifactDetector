using Emgu.CV;
using System.Drawing;
using System.Windows.Forms;

namespace ArbitraryArtifactDetector.Viewers
{
    public partial class ImageViewer : Form
    {
        public ImageViewer(Mat image)
        {
            InitializeComponent();

            Text = "DebugWindow: Show Matches";

            if (image != null)
            {
                imgBox.Image = image.Bitmap;

                Size size = image.Size;
                size.Width += 12;
                size.Height += 42;
                if (!Size.Equals(size)) Size = size;
            }
        }
    }
}