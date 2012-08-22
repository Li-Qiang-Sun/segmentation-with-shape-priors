using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Research.GraphBasedShapePrior.Util;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using Vector = Research.GraphBasedShapePrior.Util.Vector;

namespace Research.GraphBasedShapePrior.ShapeModelLearning
{
    public partial class MainWindow
    {
        private readonly List<ImageInfo> imageInfos = new List<ImageInfo>();

        private ShapeModel shapeModel;

        private ObjectBackgroundColorModels colorModels;

        private readonly AlgorithmProperties algorithmProperties = new AlgorithmProperties();

        public MainWindow()
        {
            this.InitializeComponent();

            this.propertyGrid.SelectedObject = algorithmProperties;

            Console.SetOut(new ConsoleCapture(this));
            DebugConfiguration.VerbosityLevel = VerbosityLevel.Everything;
        }

        private void UpdateControlsAccordingToCurrentState()
        {
            int selectedIndex = this.backgroundImagesListBox.SelectedIndex;
            if (selectedIndex == -1)
            {
                this.shapeEditor.Shape = null;
                this.editorTabControl.Background = null;
                this.objectMaskImage.Source = null;
            }
            else
            {
                BitmapSource image = this.imageInfos[selectedIndex].Image;
                ImageBrush backgroundBrush = new ImageBrush
                {
                    ImageSource = image,
                    Stretch = Stretch.None,
                    TileMode = TileMode.None,
                    ViewportUnits = BrushMappingMode.Absolute,
                    Viewport = new Rect(0, 0, image.PixelWidth, image.PixelHeight)
                };

                this.shapeEditor.Shape = this.imageInfos[selectedIndex].Shape;
                this.editorTabControl.Background = backgroundBrush;
                this.objectMaskImage.Source = this.imageInfos[selectedIndex].SegmentationMask;
            }

            this.saveShapeModelButton.IsEnabled = this.shapeModel != null;
            this.learnShapeModelButton.IsEnabled = this.shapeModel != null && this.imageInfos.Count > 0;

            this.saveColorModelButton.IsEnabled = this.colorModels != null;
            this.learnColorModelButton.IsEnabled = this.imageInfos.Count > 0;

            this.segmentImageButton.IsEnabled =
                this.backgroundImagesListBox.SelectedIndex != -1 && this.shapeModel != null && this.colorModels != null;
            this.segmentImageWithoutShapeButton.IsEnabled =
                this.backgroundImagesListBox.SelectedIndex != -1 && this.colorModels != null;

            this.shapeModelSpecifiedLabel.Visibility = this.shapeModel != null ? Visibility.Visible : Visibility.Hidden;
            this.shapeModelNotSpecifiedLabel.Visibility = this.shapeModel != null ? Visibility.Hidden : Visibility.Visible;
            this.colorModelSpecifiedLabel.Visibility = this.colorModels != null ? Visibility.Visible : Visibility.Hidden;
            this.colorModelNotSpecifiedLabel.Visibility = this.colorModels != null ? Visibility.Hidden : Visibility.Visible;
        }

        private void OnAddImageButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp";
            openDialog.RestoreDirectory = true;
            bool? result = openDialog.ShowDialog();
            if (result != true)
                return;

            BitmapSource image = new BitmapImage(new Uri(openDialog.FileName));
            image = ImageHelper.FixDpi(image);
            Shape shape = null;
            if (this.shapeModel != null)
                shape = this.shapeModel.FitMeanShape((int)image.Width, (int)image.Height);
            imageInfos.Add(new ImageInfo { Image = image, Shape = shape });

            this.backgroundImagesListBox.Items.Add(Path.GetFileName(openDialog.FileName));

            this.UpdateControlsAccordingToCurrentState();
        }

        private void OnRemoveImageButtonClick(object sender, RoutedEventArgs e)
        {
            int selectedIndex = this.backgroundImagesListBox.SelectedIndex;
            if (selectedIndex == -1)
                return;

            this.backgroundImagesListBox.SelectedIndex = -1;
            this.imageInfos.RemoveAt(selectedIndex);
            this.backgroundImagesListBox.Items.RemoveAt(selectedIndex);

            this.UpdateControlsAccordingToCurrentState();
        }

        private void OnBackgroundImagesListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                int prevIndex = this.backgroundImagesListBox.Items.IndexOf(e.RemovedItems[0]);
                this.imageInfos[prevIndex].ColorModelMaskEditContext = this.colorMaskEditor.SaveEditorContext();
                this.imageInfos[prevIndex].ColorModelMask = this.colorMaskEditor.GetMask(
                    this.imageInfos[prevIndex].Image.PixelWidth, this.imageInfos[prevIndex].Image.PixelHeight);
            }

            if (e.AddedItems.Count > 0)
            {
                int curIndex = this.backgroundImagesListBox.Items.IndexOf(e.AddedItems[0]);
                if (this.imageInfos[curIndex].ColorModelMaskEditContext != null)
                    this.colorMaskEditor.RestoreEditorContext(this.imageInfos[curIndex].ColorModelMaskEditContext);
                else
                    this.colorMaskEditor.ResetEditorContext();
            }

            this.UpdateControlsAccordingToCurrentState();
        }

        private void OnLoadShapeModelButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Shape models|*.shp";
            openDialog.RestoreDirectory = true;
            bool? result = openDialog.ShowDialog();
            if (result != true)
                return;

            this.shapeModel = ShapeModel.LoadFromFile(openDialog.FileName);
            for (int i = 0; i < imageInfos.Count; ++i)
            {
                Shape meanShape = this.shapeModel.FitMeanShape(imageInfos[i].Image.PixelWidth, imageInfos[i].Image.PixelHeight);
                imageInfos[i].Shape = meanShape;
            }

            this.UpdateControlsAccordingToCurrentState();
        }

        private void OnSaveShapeModelButtonClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Shape models|*.shp";
            saveDialog.RestoreDirectory = true;
            bool? result = saveDialog.ShowDialog();
            if (result != true)
                return;

            this.shapeModel.SaveToFile(saveDialog.FileName);
        }

        private void OnLearnShapeModelButtonClick(object sender, RoutedEventArgs e)
        {
            this.shapeModel = ShapeModel.Learn(
                from imageInfo in this.imageInfos select imageInfo.Shape.FitToSize(this.algorithmProperties.LearnedObjectSize, this.algorithmProperties.LearnedObjectSize),
                this.shapeModel.ConstrainedEdgePairs);

            for (int i = 0; i < this.shapeModel.Structure.Edges.Count; ++i)
            {
                ShapeEdgeParams @params = this.shapeModel.GetEdgeParams(i);
                LogMessage("Edge {0}: mean width ratio is {1:0.000}, deviation is {2:0.000}", i, @params.WidthToEdgeLengthRatio, @params.WidthToEdgeLengthRatioDeviation);
            }

            foreach (Tuple<int, int> constrainedEdgePair in this.shapeModel.ConstrainedEdgePairs)
            {
                ShapeEdgePairParams @params = this.shapeModel.GetEdgePairParams(
                    constrainedEdgePair.Item1, constrainedEdgePair.Item2);
                LogMessage("Edge pair ({0}, {1}):", constrainedEdgePair.Item1, constrainedEdgePair.Item2);
                LogMessage("mean length ratio is {0}, deviation is {1}", @params.MeanLengthRatio, @params.LengthDiffDeviation);
                LogMessage("mean angle is {0}, deviation is {1}", @params.MeanAngle, @params.AngleDeviation);
            }
        }

        private void OnLoadColorModelButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Color models|*.clr";
            openDialog.RestoreDirectory = true;
            bool? result = openDialog.ShowDialog();
            if (result != true)
                return;

            this.colorModels = ObjectBackgroundColorModels.LoadFromFile(openDialog.FileName);

            this.UpdateControlsAccordingToCurrentState();
        }

        private void OnSaveColorModelButtonClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Color models|*.clr";
            saveDialog.RestoreDirectory = true;
            bool? result = saveDialog.ShowDialog();
            if (result != true)
                return;

            this.colorModels.SaveToFile(saveDialog.FileName);
        }

        private void OnLearnColorModelButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.backgroundImagesListBox.SelectedIndex != -1)
            {
                ImageInfo currentImageInfo = this.imageInfos[this.backgroundImagesListBox.SelectedIndex];
                currentImageInfo.ColorModelMask = this.colorMaskEditor.GetMask(currentImageInfo.Image.PixelWidth, currentImageInfo.Image.PixelHeight);
            }

            List<Color> objectColors = new List<Color>();
            List<Color> backgroundColors = new List<Color>();
            for (int i = 0; i < this.imageInfos.Count; ++i)
            {
                if (this.imageInfos[i].ColorModelMask == null)
                    continue;

                Image2D<Color> image = ImageHelper.BitmapSourceToImage2D(this.imageInfos[i].Image);
                ExtractObjectBackgroundColorsByMask(
                    image,
                    this.imageInfos[i].ColorModelMask,
                    objectColors,
                    backgroundColors);

                Image2D.SaveToFile(image, string.Format("image_{0}.png", i));
                Image2D.SaveToFile(this.imageInfos[i].ColorModelMask, string.Format("mask_{0}.png", i));
            }

            if (objectColors.Count == 0)
            {
                MessageBox.Show("No object pixels specified.");
                return;
            }

            if (backgroundColors.Count == 0)
            {
                MessageBox.Show("No background pixels specified.");
                return;
            }

            Helper.Subsample(objectColors, this.algorithmProperties.MaxPixelsToLearnFrom);
            Helper.Subsample(backgroundColors, this.algorithmProperties.MaxPixelsToLearnFrom);

            GaussianMixtureColorModel objectModel = GaussianMixtureColorModel.Fit(
                objectColors.Take(this.algorithmProperties.MaxPixelsToLearnFrom),
                this.algorithmProperties.MixtureComponentCount,
                this.algorithmProperties.StopTolerance);
            GaussianMixtureColorModel backgroundModel = GaussianMixtureColorModel.Fit(
                backgroundColors.Take(this.algorithmProperties.MaxPixelsToLearnFrom),
                this.algorithmProperties.MixtureComponentCount,
                this.algorithmProperties.StopTolerance);
            this.colorModels = new ObjectBackgroundColorModels(objectModel, backgroundModel);

            this.UpdateControlsAccordingToCurrentState();
        }

        private void OnSegmentImageWithoutShapeButtonClick(object sender, RoutedEventArgs e)
        {
            this.SegmentImage((segmentator, scale) => { segmentator.ShapeUnaryTermWeight = 0; });
        }

        private void OnSegmentImageButtonClick(object sender, RoutedEventArgs e)
        {
            this.SegmentImage(
                (segmentator, scale) =>
                {
                    Shape scaledShape = this.shapeEditor.Shape.Scale(scale, Vector.Zero);
                    this.shapeModel.BackgroundDistanceCoeff = this.algorithmProperties.BackgroundDistanceCoeff;
                    segmentator.ShapeModel = this.shapeModel;
                    segmentator.Shape = scaledShape;
                });
        }

        private void SegmentImage(Action<SimpleSegmentationAlgorithm, double> customSetupStep)
        {
            int selectedIndex = this.backgroundImagesListBox.SelectedIndex;
            BitmapSource originalImage = this.imageInfos[selectedIndex].Image;

            // Compute scale for everything
            double widthScale = this.algorithmProperties.SegmentedImageSize / originalImage.PixelWidth;
            double heightScale = this.algorithmProperties.SegmentedImageSize / originalImage.PixelHeight;
            double scale = Math.Min(widthScale, heightScale);

            // Downscale original image
            BitmapSource scaledImage = ImageHelper.ResizeImage(
                originalImage, (int)(originalImage.PixelWidth * scale), (int)(originalImage.PixelHeight * scale));
            Image2D<Color> image = ImageHelper.BitmapSourceToImage2D(scaledImage);
            Image2D.SaveToFile(image, "image.png");

            // Customize segmentator
            SimpleSegmentationAlgorithm segmentator = new SimpleSegmentationAlgorithm();
            segmentator.ShapeUnaryTermWeight = this.algorithmProperties.ShapeTermWeight;
            segmentator.UnaryTermWeight = this.algorithmProperties.UnaryTermWeight;
            segmentator.ConstantBinaryTermWeight = this.algorithmProperties.ConstantBinaryTermWeight;
            segmentator.BrightnessBinaryTermCutoff = this.algorithmProperties.BrightnessBinaryTermCutoff;
            segmentator.ShapeEnergyWeight = this.algorithmProperties.ShapeEnergyWeight;

            // Run custom setup step
            customSetupStep(segmentator, scale);

            // Segment
            Image2D<bool> mask = segmentator.SegmentImage(image, this.colorModels);
            Image2D.SaveToFile(mask, "mask.png");
            BitmapSource maskImage = ImageHelper.ResizeImage(
                ImageHelper.MaskToBitmapSource(mask), originalImage.PixelWidth, originalImage.PixelHeight);
            this.imageInfos[selectedIndex].SegmentationMask = maskImage;

            // Show results
            this.UpdateControlsAccordingToCurrentState();
            this.editorTabControl.SelectedIndex = 1;
        }

        private static void ExtractObjectBackgroundColorsByMask(
            Image2D<Color> image,
            Image2D<bool?> colorModelMask,
            IList<Color> objectPixels,
            IList<Color> backgroundPixels)
        {
            Debug.Assert(image.Width == colorModelMask.Width && image.Height == colorModelMask.Height);
            for (int i = 0; i < image.Width; ++i)
                for (int j = 0; j < image.Height; ++j)
                {
                    if (!colorModelMask[i, j].HasValue)
                        continue;

                    Color pixelColor = image[i, j];
                    bool isObject = colorModelMask[i, j].Value;
                    if (isObject)
                        objectPixels.Add(pixelColor);
                    else
                        backgroundPixels.Add(pixelColor);
                }
        }

        private void LogMessage(string message)
        {
            this.logTextBox.AppendText(message);
            this.logTextBox.AppendText(Environment.NewLine);
        }

        private void LogMessage(string format, params object[] args)
        {
            string message = String.Format(format, args);
            LogMessage(message);
        }

        private class ImageInfo
        {
            public Shape Shape { get; set; }

            public BitmapSource Image { get; set; }

            public BitmapSource SegmentationMask { get; set; }

            public MaskEditContext ColorModelMaskEditContext { get; set; }

            public Image2D<bool?> ColorModelMask { get; set; }
        }

        private class ConsoleCapture : TextWriter
        {
            private readonly MainWindow master;
            private readonly StringBuilder line = new StringBuilder();

            public ConsoleCapture(MainWindow master)
            {
                this.master = master;
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

            public override void Write(char value)
            {
                lock (this.master)
                {
                    if (value != '\n')
                        line.Append(value);
                    else
                    {
                        string result = line.ToString().TrimEnd('\r');
                        this.master.Dispatcher.Invoke(new Action(() => this.master.LogMessage(result)));
                        line.Length = 0;
                    }
                }
            }
        }
    }
}
