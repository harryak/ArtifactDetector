using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;

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

            Text = "Debug Window: Showing visual feature matches";

            if (image != null)
            {
                pictureBox.Image = image.ToImage<Bgr, byte>().AsBitmap();
                Size = image.Size;
            }
        }
    }
}
