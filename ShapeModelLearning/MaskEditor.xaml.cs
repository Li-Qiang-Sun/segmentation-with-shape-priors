using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Research.GraphBasedShapePrior.Util;
using Color = System.Drawing.Color;

namespace Research.GraphBasedShapePrior.ShapeModelLearning
{
    public class MaskEditContext
    {
        private readonly List<Stroke> strokes;
        
        public MaskEditContext(IEnumerable<Stroke> strokes)
        {
            this.strokes = new List<Stroke>(strokes);
        }

        public ReadOnlyCollection<Stroke> Strokes
        {
            get { return this.strokes.AsReadOnly(); } 
        }
    }
    
    public partial class MaskEditor
    {
        public MaskEditor()
        {
            InitializeComponent();
        }

        public MaskEditContext SaveEditorContext()
        {
            return new MaskEditContext(this.inkCanvas.Strokes);
        }

        public void RestoreEditorContext(MaskEditContext context)
        {
            this.ResetEditorContext();
            this.inkCanvas.Strokes.Add(new StrokeCollection(context.Strokes));
        }

        public void ResetEditorContext()
        {
            this.inkCanvas.Strokes.Clear();
        }

        public Image2D<bool?> GetMask(int maskWidth, int maskHeight)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap(maskWidth, maskHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(this.inkCanvas);
            Image2D<Color> maskImage = ImageHelper.BitmapSourceToImage2D(rtb);
            Image2D<bool?> result = new Image2D<bool?>(maskImage.Width, maskImage.Height);

            const int colorIntensityThreshold = 50;
            for (int i = 0; i < maskImage.Width; ++i)
                for (int j = 0; j < maskImage.Height; ++j)
                {
                    Color maskColor = maskImage[i, j];
                    if (maskColor.G > colorIntensityThreshold)
                        result[i, j] = true;
                    else if (maskColor.B > colorIntensityThreshold)
                        result[i, j] = false;
                    else
                        result[i, j] = null;
                }

            return result;
        }

        private void OnClearMasksButtonClick(object sender, RoutedEventArgs e)
        {
            this.ResetEditorContext();
        }

        private void OnMarkObjectRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            this.inkCanvas.DefaultDrawingAttributes = (DrawingAttributes)this.FindResource("ObjectDrawingAttributes");
        }

        private void OnMarkBackgroundRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            this.inkCanvas.DefaultDrawingAttributes = (DrawingAttributes)this.FindResource("BackgroundDrawingAttributes");
        }
    }
}
