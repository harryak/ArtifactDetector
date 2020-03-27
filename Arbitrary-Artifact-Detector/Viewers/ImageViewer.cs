using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;

namespace ItsApe.ArtifactDetector.Viewers
{
    /// <summary>
    /// Actually a debug utility but has to reside in this namespace for .NET.
    /// 
    /// Used to view EmguCV Mats.
    /// </summary>
    public partial class ImageViewer : Form
    {
        /// <summary>
        /// Open window displaying the given Mat.
        /// </summary>
        /// <param name="image"></param>
        public ImageViewer(Mat image)
        {
            InitializeComponent();

            Text = "DebugWindow: Show Matches";

            if (image != null)
            {
                imgBox.Image = image.Bitmap;

                var size = image.Size;
                size.Width += 12;
                size.Height += 42;
                if (!Size.Equals(size)) Size = size;
            }
        }
    }
}
