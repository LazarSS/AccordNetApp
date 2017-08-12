using Accord.Imaging;
using Accord.Imaging.Converters;
using Accord.Imaging.Filters;
using Accord.MachineLearning;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Math.Distances;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Kernels;
using Accord.Statistics.Models.Regression.Linear;
using MLImaging.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MLImaging
{

    public partial class MainWindow : Window
    {
        private bool imageLoaded = false;
        private String imageString = null;
        private Bitmap cvsbmp = null, old = null;

        public MainWindow()
        {
            InitializeComponent();
            imageInfo.Text = "";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog op = new Microsoft.Win32.OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                cvsim.Source = new BitmapImage(new Uri(op.FileName));

                cvs.Width = cvsim.Source.Width;
                cvs.Height = cvsim.Source.Height;
                imageLoaded = true;
                imageString = op.FileName;
                if (cvs.Children.Count > 1)
                    cvs.Children.RemoveRange(1, cvs.Children.Count);
                imageInfo.Text = "";

            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (imageLoaded)
            {
                var cascade = new Accord.Vision.Detection.Cascades.FaceHaarCascade();

                var detector = new Accord.Vision.Detection.HaarObjectDetector(cascade, minSize: 50,
                    searchMode: Accord.Vision.Detection.ObjectDetectorSearchMode.Average);

                cvsbmp = UtilFn.getBmpFromCvsim(cvsim);
                System.Drawing.Rectangle[] rectangles = detector.ProcessFrame(cvsbmp);

                foreach (System.Drawing.Rectangle r in rectangles)
                {
                    imageInfo.Text += "Left:" + r.Left + " Top:" + r.Top + " Width:" + r.Width + " Height:" + r.Height + ";\n";

                    System.Windows.Shapes.Rectangle rect;
                    rect = new System.Windows.Shapes.Rectangle();
                    rect.Stroke = new SolidColorBrush(Colors.Violet);
                    rect.StrokeThickness = 5;

                    rect.Width = r.Width;
                    rect.Height = r.Height;
                    Canvas.SetLeft(rect, r.Left);
                    Canvas.SetTop(rect, r.Top);
                    cvs.Children.Add(rect);
                }
                if (rectangles.Count() == 0)
                    imageInfo.Text = "No faces detected!";
            }
            else
            {
                imageInfo.Text = "Image not loaded!";
            }
        }


        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (imageLoaded)
            {
                cvsbmp = UtilFn.getBmpFromCvsim(cvsim);

                Grayscale gfilter = new Grayscale(0.2125, 0.7154, 0.0721);

                Bitmap grayImage = gfilter.Apply(cvsbmp);

                BorderFollowing bf = new BorderFollowing();

                List<Accord.IntPoint> contour = bf.FindContour(cvsbmp);


                //Bitmap output = new PointsMarker(contour, System.Drawing.Color.Blue,5).Apply(cvsbmp);

                //WPFBitmapConverter converter = new WPFBitmapConverter();
                //cvsim.Source = converter.Convert(output, Type.GetType("BitmapImage"), null, null) as BitmapImage;

                imageInfo.Text = contour.Count + ": ";


                foreach (var c in contour)
                {
                    Ellipse dot = new Ellipse();
                    dot.Width = 4;
                    dot.Height = 4;
                    dot.StrokeThickness = 2;
                    dot.Stroke = System.Windows.Media.Brushes.Violet;
                    Canvas.SetTop(dot, c.Y - 1);
                    Canvas.SetLeft(dot, c.X - 1);
                    imageInfo.Text += "(" + c.X + ", " + c.Y + ");";
                    cvs.Children.Add(dot);
                }

            }
            else
            {
                imageInfo.Text = "Image not loaded!";
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (imageLoaded)
            {
                cvsbmp = UtilFn.getBmpFromCvsim(cvsim);
                old = cvsbmp;

                Grayscale gfilter = new Grayscale(0.2125, 0.7154, 0.0721);

                Bitmap grayImage = gfilter.Apply(cvsbmp);

                GaborFilter filter = new GaborFilter();

                Bitmap output = filter.Apply(grayImage);

                WPFBitmapConverter converter = new WPFBitmapConverter();
                cvsim.Source = converter.Convert(output, Type.GetType("BitmapImage"), null, null) as BitmapImage;
            }
            else
            {
                imageInfo.Text = "Image not loaded!";
            }
        }
        private void button_Click_4(object sender, RoutedEventArgs e)
        {
            if (imageLoaded)
            {
                Bitmap image = UtilFn.getBmpFromCvsim(cvsim);
                old = image;
                var imageToArray = new ImageToArray(min: -1, max: +1);
                var arrayToImage = new ArrayToImage(image.Width, image.Height, min: -1, max: +1);

                double[][] pixels; imageToArray.Convert(image, out pixels);

                KMeans kmeans = new KMeans(k: 5)
                {
                    Distance = new SquareEuclidean(),
                    Tolerance = 0.05
                };

                var clusters = kmeans.Learn(pixels);

                int[] labels = clusters.Decide(pixels);

                double[][] replaced = pixels.Apply((x, i) => clusters.Centroids[labels[i]]);

                Bitmap result; arrayToImage.Convert(replaced, out result);

                WPFBitmapConverter converter = new WPFBitmapConverter();
                cvsim.Source = converter.Convert(result, Type.GetType("BitmapImage"), null, null) as BitmapImage;
            }
            else
            {
                imageInfo.Text = "Image not loaded!";
            }
        }
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (old != null)
            {
                WPFBitmapConverter converter = new WPFBitmapConverter();
                cvsim.Source = converter.Convert(old, Type.GetType("BitmapImage"), null, null) as BitmapImage;
            }
            cvs.Children.RemoveRange(1, cvs.Children.Count - 1);
            imageInfo.Text = "";
        }






        // ML TAB

        Dictionary<bool, List<System.Windows.Point>> classPoints = new Dictionary<bool, List<System.Windows.Point>>
        {
            {false, new List<System.Windows.Point>()}, // plave
            {true, new List<System.Windows.Point>()}  // crvene
        };

        SolidColorBrush redBrush = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(255, 0, 0) };
        SolidColorBrush blueBrush = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(0, 0, 255) };
        SolidColorBrush greenBrush = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(0, 255, 0) };
        SolidColorBrush blackBrush = new SolidColorBrush() { Color = System.Windows.Media.Color.FromRgb(0, 0, 0) };

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(cvsML);

            Ellipse dot = new Ellipse();
            dot.Width = 9;
            dot.Height = 9;
            dot.StrokeThickness = 2;
            Canvas.SetTop(dot, p.Y - 4);
            Canvas.SetLeft(dot, p.X - 4);

            if (rbBlue.IsChecked != null && rbBlue.IsChecked == true)
            {
                classPoints[false].Add(p);
                dot.Fill = blueBrush;
            }
            else
            {
                classPoints[true].Add(p);
                dot.Fill = redBrush;
            }

            cvsML.Children.Add(dot);
        }

        private void bClear_Click(object sender, RoutedEventArgs e)
        {
            classPoints[false] = new List<System.Windows.Point>();
            classPoints[true] = new List<System.Windows.Point>();
            cvsML.Children.RemoveRange(0, cvsML.Children.Count);
        }

        private void bReg_Click(object sender, RoutedEventArgs e)
        {
            // radi regresiju po plavim i crvenim tackama odvojeno

            var ols = new OrdinaryLeastSquares()
            {
                UseIntercept = true
            };

            int nB = classPoints[false].Count;
            int nR = classPoints[true].Count;

            double[] inputsRed = new double[nR];
            double[] inputsBlue = new double[nB];
            double[] outputsRed = new double[nR];
            double[] outputsBlue = new double[nB];

            int i = 0;
            foreach (System.Windows.Point p in classPoints[false])
            {
                outputsBlue[i] = p.Y;
                inputsBlue[i++] = p.X;
            }

            i = 0;
            foreach (System.Windows.Point p in classPoints[true])
            {
                outputsRed[i] = p.Y;
                inputsRed[i++] = p.X;
            }

            Accord.Statistics.Models.Regression.Linear.SimpleLinearRegression regression;
            double k, b;

            if (nB > 0)
            {
                regression = ols.Learn(inputsBlue, outputsBlue);

                k = regression.Slope;
                b = regression.Intercept;

                Line line = new Line();
                line.Stroke = System.Windows.Media.Brushes.Violet;

                line.X1 = 0;
                line.Y1 = b;

                line.X2 = cvsML.ActualWidth;
                line.Y2 = k * cvsML.ActualWidth + b;
                line.StrokeThickness = 5;
                cvsML.Children.Add(line);
            }
            else
            {
                textBlock.Text = "No blue dots.";

            }
            if (nR > 0)
            {
                regression = ols.Learn(inputsRed, outputsRed);
                k = regression.Slope;
                b = regression.Intercept;

                Line line1 = new Line();
                line1.Stroke = System.Windows.Media.Brushes.Violet;

                line1.X1 = 0;
                line1.Y1 = b;

                line1.X2 = cvsML.ActualWidth;
                line1.Y2 = k * cvsML.ActualWidth + b;
                line1.StrokeThickness = 5;
                cvsML.Children.Add(line1);
            }
            else
            {
                textBlock.Text = "No red dots.";

            }
        }

        private void bClass_Click(object sender, RoutedEventArgs e)
        {
            int nB = classPoints[false].Count;
            int nR = classPoints[true].Count;

            double sigma = Double.Parse(sigmaBox.Text);

            if (nR < 1 || nB < 1)
            {
                textBlock.Text = "You need at least one dot in each class";
                return;
            }

            double[][] inputs = new double[nB + nR][];
            int[] outputs = new int[nR + nB];

            int i = 0;
            foreach (System.Windows.Point p in classPoints[false])
            {
                outputs[i] = 0;
                inputs[i++] = new double[] { p.X, p.Y };
            }

            foreach (System.Windows.Point p in classPoints[true])
            {
                outputs[i] = 1;
                inputs[i++] = new double[] { p.X, p.Y };

            }

            SupportVectorMachine<IKernel> svm;

            var smo = new SequentialMinimalOptimization<IKernel>()
            {
                Kernel = new Linear(sigma)
            };

            svm = smo.Learn(inputs, outputs);

            foreach (System.Windows.Point p in classPoints[false]) //za sve plave
            {
                var z = svm.Decide(new double[] { p.X, p.Y });

                if (z == true) //reko da je crvena
                {
                    Ellipse dot = new Ellipse();
                    dot.Width = 11;
                    dot.Height = 11;
                    dot.StrokeThickness = 2;
                    dot.Stroke = redBrush;
                    Canvas.SetTop(dot, p.Y - 5);
                    Canvas.SetLeft(dot, p.X - 5);

                    cvsML.Children.Add(dot);
                }

            }
            foreach (System.Windows.Point p in classPoints[true]) // za sve crvene
            {
                var z = svm.Decide(new double[] { p.X, p.Y });

                if (z == false) //reko da je plava
                {
                    Ellipse dot = new Ellipse();
                    dot.Width = 11;
                    dot.Height = 11;
                    dot.StrokeThickness = 2;
                    dot.Stroke = blueBrush;
                    Canvas.SetTop(dot, p.Y - 5);
                    Canvas.SetLeft(dot, p.X - 5);

                    cvsML.Children.Add(dot);
                }
            }
        }

        private void bClus_Click(object sender, RoutedEventArgs e)
        {

            int k = Int16.Parse(kKmeans.Text);
            int nB = classPoints[false].Count;
            int nR = classPoints[true].Count;

            if (nR + nB < 1)
            {
                textBlock.Text = "No dots.";
                return;
            }
            if (k < 1)
            {
                textBlock.Text = "Number of classes must be > 0.";
                return;          
            }

            double[][] inputs = new double[nB + nR][];

            int i = 0;
            foreach (System.Windows.Point p in classPoints[false])
            {
                inputs[i++] = new double[] { p.X, p.Y };
            }

            foreach (System.Windows.Point p in classPoints[true])
            {
                inputs[i++] = new double[] { p.X, p.Y };

            }

            KMeans kmeans = new KMeans(k: k);

            var clusters = kmeans.Learn(inputs);
            int[] labels = clusters.Decide(inputs);

            var centroids = clusters.Centroids;
            for (int j = 0; j < k; ++j)
            {
                Ellipse dot = new Ellipse();
                dot.Width = 11;
                dot.Height = 11;
                dot.StrokeThickness = 2;
                dot.Fill = blackBrush;
                Canvas.SetTop(dot, centroids[j][1] - 5);
                Canvas.SetLeft(dot, centroids[j][0] - 5);

                cvsML.Children.Add(dot);

                double max = 0;

                for (int z = 0; z < labels.Length; ++z)
                {
                    if (labels[z] == j)
                    {
                        double d = Math.Sqrt(Math.Pow(centroids[j][0] - inputs[z][0], 2) + Math.Pow(centroids[j][1] - inputs[z][1], 2));
                        if (d > max)
                            max = d;
                    }
                }

                Ellipse circle = new Ellipse();
                circle.Width = 2 * max + 10;
                circle.Height = 2 * max + 10;
                circle.StrokeThickness = 2;
                circle.Stroke = blackBrush;
                Canvas.SetTop(circle, centroids[j][1] - max - 5);
                Canvas.SetLeft(circle, centroids[j][0] - max - 5);

                cvsML.Children.Add(circle);
            }
        }


    }
}
