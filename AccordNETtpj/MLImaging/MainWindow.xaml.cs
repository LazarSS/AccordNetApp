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
using WPFFolderBrowser;

namespace MLImaging
{

    public partial class MainWindow : Window
    {
        private Bitmap cvsbmp = null;
        bool hoverOr = true, hoverRes = false;
        List<BitmapImage> images = new List<BitmapImage>();
        List<BitmapImage> imagesEdited = new List<BitmapImage>();
        bool[] selected, output;
        WPFBitmapConverter converter = new WPFBitmapConverter();
        System.Windows.Shapes.Rectangle rectSel, rectSel2;
        string helpText = "Imaging\n" +
            "Checkbox Folder denotes whether Folder or File dialog should be opened. If it's unchecked, Select All/Current checkbox is disabled, because only one image is selected.\n" +
            "Face button detects faces on loaded image(s), using basic Haar-like features. Detected faces are cropped and merged, and then displayed next to the corresponding original image.\n" +
            "Border button detects a border of selected image(s). It should be applied on transparent-background and simple images. Result is a pink line which is being drawn over the copy of loaded image.\n" +
            "Edge button detects edges on the grayscale version of loaded image. Resulting image is created with filter already applied.\n" +
            "Cluster button converts loaded image(s) into double array of pixel values between -1 and 1. Values are then clustered using K-means algorithm and a given number of clusters, and pixel values replaced with those of their cluster's centroid.\n" +
            "Select All/Free checkbox represents whether transformation should be applied to all loaded images or only the one currently displayed. This checkbox is unchecked and disabled if only one image is loaded. When it comes to resulting images, it is used for labeling during SVM training, when estimating performance of SVM, or selecting results that will later be saved.\n" +
            "Save button saves resulting image(s) (selected) to folder selected through dialog.\n" +
            "BoW model is used for feature extraction. It is trained upon raw Bitmaps and is later on able to produce vectors (dimension 20 hardcoded) for new Bitmaps. Those vectors are then used for SVM training.\n" +
            "SVM is simple Linear-kernel SVM. It is used for binary classification and selected images (resulting) are labeled +1. Its complexity (constant C) is fixed to 70. It hardens the margin (must experiment).\n" +
            "Predict All makes prediction upon all resulting images (loaded or after transformations) when BoW and SVM models are loaded.\n" +
            "% Correctnes gives precission and recall values after the prediction. User should first label +1 class images himself.\n" +
            "HOVER AND KEYS : You can hover over the image (whether resulting or original) and then use keys Left and Right for swapping, and Numpad0 for selection (selection works only on resulting images).\n\n" +
            "ML\n" +
            "Regression button does regression upon blue and red dots placed on canvas (two regression lines). You need at least one dot of any color.\n" +
            "Classification separates blue and red dots (you need at leat one dot of each class) using SVM with linear kernel. Outliers are then encircled with the color of the class they 'should be in'. Sigma parameter denotes kernel function's intercept point." +
            "\nK-means takes in all the dots, and clusters them in a given number of clusters. Colors don't matter.";
        ushort i = 0, j = 0;
        string filters = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                  "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                  "Portable Network Graphic (*.png)|*.png";
        String[] filtersDir = new String[] { "jpg", "jpeg", "png", "gif", "tiff", "bmp" };
        BagOfVisualWords bow = null;
        SupportVectorMachine<Linear> svmIm = null;

        public MainWindow()
        {
            InitializeComponent();
            imageInfo.Text = "";

            leftOriginal.IsEnabled = false;
            rightOriginal.IsEnabled = false;
            leftEdited.IsEnabled = false;
            rightEdited.IsEnabled = false;
            helpTB.Text = helpText;
            allcurr.IsChecked = false;
            allcurr.IsEnabled = false;
            btnCorrect.IsEnabled = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            images = new List<BitmapImage>();
            imagesEdited = new List<BitmapImage>();
            i = 0;j = 0;
            Boolean dialogOK = true;
            if (checkBox.IsChecked == true)
            {
                allcurr.IsChecked = false;
                allcurr.IsEnabled = true;
                WPFFolderBrowserDialog dd = new WPFFolderBrowserDialog();
                dd.Title = "Select a folder";

                if (dd.ShowDialog() == true)
                {
                    String[] names = UtilFn.GetFilesFrom(dd.FileName, filtersDir, false);
                    if (names.Length == 0)
                        return;
                    foreach (String fn in names)
                    {
                        images.Add(new BitmapImage(new Uri(fn)));
                        imagesEdited.Add(new BitmapImage(new Uri(fn)));

                    }
                }
                else
                {
                    dialogOK = false;
                }
            }
            else
            {
                allcurr.IsChecked = false;
                allcurr.IsEnabled = false;
                Microsoft.Win32.OpenFileDialog op = new Microsoft.Win32.OpenFileDialog();
                op.Title = "Select a picture";
                op.Filter = filters;
                if (op.ShowDialog() == true)
                {
                    images.Add(new BitmapImage(new Uri(op.FileName)));
                    imagesEdited.Add(new BitmapImage(new Uri(op.FileName)));
                }
                else
                {
                    dialogOK = false;
                }
            }

            if (!dialogOK)
            {
                allcurr.IsChecked = false;
                allcurr.IsEnabled = false;
                MessageBox.Show("Dialog fail");
                return;
            }

            if (0 == images.Count)
            {
                allcurr.IsChecked = false;
                allcurr.IsEnabled = false;
                MessageBox.Show("No images");
                return;
            }

            cvsim.Source = imagesEdited[i];
            cvsim2.Source = images[i];
            if (1 == images.Count)
            {
                rightOriginal.IsEnabled = false;
                rightEdited.IsEnabled = false;
            }
            else
            {
                rightOriginal.IsEnabled = true;
                rightEdited.IsEnabled = true;
            }
            cvs.Width = cvsim.Source.Width;
            cvs.Height = cvsim.Source.Height;
            cvs2.Width = cvsim2.Source.Width;
            cvs2.Height = cvsim2.Source.Height;

            rectSel = new System.Windows.Shapes.Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 4,
                Width = cvs.Width,
                Height = cvs.Height
            };

            Canvas.SetLeft(rectSel, 0);
            Canvas.SetTop(rectSel, 0);

            rectSel2 = new System.Windows.Shapes.Rectangle
            {
                Stroke = new SolidColorBrush(Colors.LimeGreen),
                StrokeThickness = 4,
                Width = cvs.Width - 20,
                Height = cvs.Height - 20
            };

            Canvas.SetLeft(rectSel2, 10);
            Canvas.SetTop(rectSel2, 10);

            if (cvs.Children.Count > 1)
                cvs.Children.RemoveRange(1, cvs.Children.Count);


            selected = new bool[imagesEdited.Count];
            //stavi sve na false
            imageInfo.Text = "";
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (images.Count > 0)
            {
                imageInfo.Text = "Working on it!";
                imagesEdited.Clear();
                this.j = 0;
                if (allcurr.IsChecked == true)
                    for (ushort i = 0; i < images.Count; ++i)
                        faceDetect(i);

                else
                    faceDetect(i);

                selected = new bool[imagesEdited.Count];
                cvsim.Source = imagesEdited[j];
                rightEdited.IsEnabled = imagesEdited.Count > 1;

                imageInfo.Text = "Done!";
            }
            else
            {
                imageInfo.Text = "Image not loaded!";
            }
        }
        private void faceDetect(ushort j)
        {
            cvsbmp = UtilFn.BitmapImage2Bitmap(images[j]);

            var cascade = new Accord.Vision.Detection.Cascades.FaceHaarCascade();

            var detector = new Accord.Vision.Detection.HaarObjectDetector(cascade, minSize: 50,
                searchMode: Accord.Vision.Detection.ObjectDetectorSearchMode.Average);


            System.Drawing.Rectangle[] rectangles = detector.ProcessFrame(cvsbmp);
            List<Bitmap> listBitmap = new List<Bitmap>();
            foreach (System.Drawing.Rectangle r in rectangles)
                listBitmap.Add(UtilFn.CropImage(cvsbmp, r.X, r.Y, r.Width, r.Height));

            if (rectangles.Count() == 0)
                imageInfo.Text = "No faces detected!";
            else
            {

                //imagesEdited[j] = (converter.Convert(UtilFn.MergeImages(listBitmap), Type.GetType("BitmapImage"), null, null) as BitmapImage).Clone();
                foreach (var x in listBitmap)
                    imagesEdited.Add((converter.Convert(x, Type.GetType("BitmapImage"), null, null) as BitmapImage).Clone());
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            imagesEdited = new List<BitmapImage>(images);
            if (images.Count > 0)
            {
                imageInfo.Text = "Working on it!";
                imagesEdited.Clear();
                this.j = 0;
                if (allcurr.IsChecked == true)
                    for (ushort i = 0; i < images.Count; ++i)
                    {
                        cvs.Children.RemoveRange(1, cvs.Children.Count);
                        cvsim.Source = imagesEdited[i];
                        borderFollow(i);
                    }
                else
                {
                    cvs.Children.RemoveRange(1, cvs.Children.Count);
                    cvsim.Source = imagesEdited[i];
                    borderFollow(i);
                }
                cvsim.Source = imagesEdited[j];
                rightEdited.IsEnabled = imagesEdited.Count > 1;

                imageInfo.Text = "Done!";
            }
            else
            {
                imageInfo.Text = "Image not loaded!";
            }
        }
        private void borderFollow(ushort j)
        {

            cvsbmp = UtilFn.BitmapImage2Bitmap(images[j]);
            Bitmap resbmp = UtilFn.ResizeBitmap(cvsbmp, (int)cvsim.Width, (int)(cvsim.Width * images[j].Height / images[j].Width));
            Grayscale gfilter = new Grayscale(0.2125, 0.7154, 0.0721);

            Bitmap grayImage = gfilter.Apply(cvsbmp);

            BorderFollowing bf = new BorderFollowing();

            List<Accord.IntPoint> contour = bf.FindContour(resbmp);

            imageInfo.Text = contour.Count + ": ";
            foreach (var c in contour)
            {
                Ellipse dot = new Ellipse();
                dot.Width = 4;
                dot.Height = 4;
                dot.StrokeThickness = 2;
                dot.Stroke = System.Windows.Media.Brushes.Violet;
                Canvas.SetTop(dot, c.Y - 1 + (int)((cvs.Height - resbmp.Height) / 2));
                Canvas.SetLeft(dot, c.X - 1);
                imageInfo.Text += "(" + c.X + ", " + c.Y + ");";
                cvs.Children.Add(dot);
            }

            imagesEdited.Add(UtilFn.rtbToBitmapImage(UtilFn.ExportToPng(null, cvs)).Clone());
            cvs.Children.RemoveRange(1, cvs.Children.Count);
        }
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {

            if (images.Count > 0)
            {
                imageInfo.Text = "Working on it!";
                imagesEdited.Clear();
                this.j = 0;
                if (allcurr.IsChecked == true)
                    for (ushort i = 0; i < images.Count; ++i)
                        edgeDetect(i);
                else
                    edgeDetect(i);

                cvsim.Source = imagesEdited[i];
                rightEdited.IsEnabled = imagesEdited.Count > 1;
                imageInfo.Text = "Done!";
            }
            else
            {
                imageInfo.Text = "Image not loaded!";
            }
        }

        private void edgeDetect(ushort j)
        {
            cvsbmp = UtilFn.BitmapImage2Bitmap(images[j]);

            Grayscale gfilter = new Grayscale(0.2125, 0.7154, 0.0721);

            Bitmap grayImage = gfilter.Apply(cvsbmp);

            GaborFilter filter = new GaborFilter();

            Bitmap output = filter.Apply(grayImage);

            imagesEdited.Add(converter.Convert(output, Type.GetType("BitmapImage"), null, null) as BitmapImage);
        }
        private void button_Click_4(object sender, RoutedEventArgs e)
        {
            imageInfo.Text = "Working on it!";
            imagesEdited.Clear();
            this.j = 0;
            if (images.Count > 0)
            {
                imagesEdited.Clear();
                this.j = 0;
                if (allcurr.IsChecked == true)
                    for (ushort i = 0; i < images.Count; ++i)
                        cluster(i);
                else
                    cluster(i);

                cvsim.Source = imagesEdited[j];
                rightEdited.IsEnabled = imagesEdited.Count > 1;

                imageInfo.Text = "Done!";

            }
            else
            {
                imageInfo.Text = "Image not loaded!";
            }
        }

        private void cluster(ushort j)
        {
            cvsbmp = UtilFn.BitmapImage2Bitmap(images[j]);
            var imageToArray = new ImageToArray(min: -1, max: +1);
            var arrayToImage = new ArrayToImage(cvsbmp.Width, cvsbmp.Height, min: -1, max: +1);
            int kk;

            double[][] pixels; imageToArray.Convert(cvsbmp, out pixels);
            try
            {
                kk = Int16.Parse(kCluster.Text);
            }
            catch (Exception e)
            {
                return;
            }

            if (kk < 1)
                return;
            KMeans kmeans = new KMeans(k: kk)
            {
                Distance = new SquareEuclidean(),
                Tolerance = 0.05
            };

            var clusters = kmeans.Learn(pixels);

            int[] labels = clusters.Decide(pixels);

            double[][] replaced = pixels.Apply((x, i) => clusters.Centroids[labels[i]]);

            Bitmap result; arrayToImage.Convert(replaced, out result);

            imagesEdited.Add(converter.Convert(result, Type.GetType("BitmapImage"), null, null) as BitmapImage);

        }

        private void rightOriginal_Click(object sender, RoutedEventArgs e)
        {
            if (i < 0 || i > images.Count - 1) return;

            cvsim2.Source = images[++i];

            if (i + 1 == images.Count)
                rightOriginal.IsEnabled = false;
            if (i > 0)
                leftOriginal.IsEnabled = true;

        }
        private void leftOriginal_Click(object sender, RoutedEventArgs e)
        {
            if (i < 0 || i > images.Count - 1) return;

            cvsim2.Source = images[--i];

            if (i - 1 == -1)
                leftOriginal.IsEnabled = false;
            if (i < images.Count - 1)
                rightOriginal.IsEnabled = true;


        }

        private void rightEdited_Click(object sender, RoutedEventArgs e)
        {
            if (j < 0 || j > imagesEdited.Count - 1) return;
            cvsim.Source = imagesEdited[++j];
            if (selected[j] && !cvs.Children.Contains(rectSel))
                cvs.Children.Add(rectSel);
            else if (!selected[j] && cvs.Children.Contains(rectSel))
                cvs.Children.Remove(rectSel);

            if (output != null)
                if (output[j] && !cvs.Children.Contains(rectSel2))
                    cvs.Children.Add(rectSel2);
                else if (!output[j] && cvs.Children.Contains(rectSel2))
                    cvs.Children.Remove(rectSel2);

            if (j + 1 == imagesEdited.Count)
                rightEdited.IsEnabled = false;
            if (j > 0)
                leftEdited.IsEnabled = true;

        }
        private void leftEdited_Click(object sender, RoutedEventArgs e)
        {
            if (j < 0 || j > imagesEdited.Count - 1) return;

            cvsim.Source = imagesEdited[--j];
            if (selected[j] && !cvs.Children.Contains(rectSel))
                cvs.Children.Add(rectSel);
            else if (!selected[j] && cvs.Children.Contains(rectSel))
                cvs.Children.Remove(rectSel);
            if (output != null)
                if (output[j] && !cvs.Children.Contains(rectSel2))
                    cvs.Children.Add(rectSel2);
                else if (!output[j] && cvs.Children.Contains(rectSel2))
                    cvs.Children.Remove(rectSel2);

            if (j - 1 == -1)
                leftEdited.IsEnabled = false;
            if (j < images.Count - 1)
                rightEdited.IsEnabled = true;


        }
        private void bSave_Click(object sender, RoutedEventArgs e)
        {
            WPFFolderBrowserDialog dd = new WPFFolderBrowserDialog();
            if (dd.ShowDialog() == true)
            {
                for (ushort i = 0; i < imagesEdited.Count; ++i)
                    if (selected[i])
                    {

                        using (FileStream stream = new FileStream(dd.FileName + "/" + i + ".png", FileMode.Create))
                        {
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(imagesEdited[i]));
                            encoder.Save(stream);
                        }
                    }
                MessageBox.Show("Done");
            }
        }
        private void cvsim_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (imagesEdited.Count < 1) return;
            selected[j] = !selected[j];
            if (selected[j] && !cvs.Children.Contains(rectSel))
                cvs.Children.Add(rectSel);
            else if (!selected[j] && cvs.Children.Contains(rectSel))
                cvs.Children.Remove(rectSel);
        }


        private void CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            for (ushort i = 0; i < imagesEdited.Count; ++i)
                selected[i] = allcurr.IsChecked.GetValueOrDefault();
            if (rectSel != null)
                if (selected[j] && !cvs.Children.Contains(rectSel))
                    cvs.Children.Add(rectSel);
                else if (!selected[j] && cvs.Children.Contains(rectSel))
                    cvs.Children.Remove(rectSel);
        }

        private void trainBoW(object sender, RoutedEventArgs e)
        {
            //train bow
            imageInfo.Text = "Training BoW";

            WPFFolderBrowserDialog dd = new WPFFolderBrowserDialog();
            dd.Title = "Select a folder";
            if (dd.ShowDialog() == true)
            {
                String[] names = UtilFn.GetFilesFrom(dd.FileName, filtersDir, false);
                if (names.Length == 0)
                    MessageBox.Show("NO IMAGES IN SELECTED FOLDER.");

                ushort c = 0;
                List<Bitmap> ims = new List<Bitmap>();
                foreach (String fn in names)
                {
                    if (c++ == 2000)
                    {
                        break;
                    }

                    ims.Add(new Bitmap(fn));
                }

                bow = new BagOfVisualWords(20); // br features
                bow.Learn(ims.ToArray());

            }
            else
            {
                MessageBox.Show("Something went wrong.");
            }
            imageInfo.Text = "Done";

        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            imageInfo.Text = "Loading BoW";
            Microsoft.Win32.OpenFileDialog op = new Microsoft.Win32.OpenFileDialog();
            op.Title = "Select a BoW file.";
            if (op.ShowDialog() == true)
            {
                bow = Accord.IO.Serializer.Load<BagOfVisualWords>(op.FileName);

            }
            else
            {
                MessageBox.Show("Something went wrong while loadin BoW!");
            }
            imageInfo.Text = "Done";

        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            imageInfo.Text = "Loading BoW";
            Microsoft.Win32.OpenFileDialog op = new Microsoft.Win32.OpenFileDialog();
            op.Title = "Select a SVM file.";
            if (op.ShowDialog() == true)
            {
                svmIm = Accord.IO.Serializer.Load<SupportVectorMachine<Linear>>(op.FileName);

            }
            else
            {
                MessageBox.Show("Something went wrong while loadin SVM!");
            }
            imageInfo.Text = "Done";
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            WPFFolderBrowserDialog dd = new WPFFolderBrowserDialog();
            dd.Title = "Select a folder";
            if (dd.ShowDialog() == true)
            {
                if (bow != null)
                    Accord.IO.Serializer.Save<SupportVectorMachine<Linear>>(svmIm, dd.FileName + "/svmModel");
                else
                    MessageBox.Show("BoW model does not exist!");
            }
            else
            {
                MessageBox.Show("Something went wrong.");
            }
            imageInfo.Text = "Done";
        }

        private void vb_MouseEnter(object sender, MouseEventArgs e)
        {
            hoverRes = true;
            hoverOr = false;
        }

        private void vb2_MouseEnter(object sender, MouseEventArgs e)
        {
            hoverRes = false;
            hoverOr = true;
        }

        private void vb2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                if (hoverOr)
                {
                    if (leftOriginal.IsEnabled)
                        leftOriginal_Click(null, null);
                }
                else
                {
                    if (leftEdited.IsEnabled)

                        leftEdited_Click(null, null);
                }
            }
            else if (e.Key == Key.Right)
            {
                if (hoverOr)
                {
                    if (rightOriginal.IsEnabled)

                        rightOriginal_Click(null, null);
                }
                else
                {
                    if (rightEdited.IsEnabled)

                        rightEdited_Click(null, null);
                }
            }
            else if (e.Key == Key.NumPad0)
            {
                cvsim_MouseUp(null, null);
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            double both = 0, sel = 0, pred = 0;
            for (ushort z = 0; z < imagesEdited.Count; ++z)
            {
                both += selected[z] && output[z] ? 1 : 0;
                sel += selected[z] ? 1 : 0;
                pred += output[z] ? 1 : 0;
            }
            imageInfo.Text = "Recall: " + both / sel + "  Precision: " + both / pred;

            btnCorrect.IsEnabled = false;
        }

        private void PredictImages(object sender, RoutedEventArgs e)
        {
            imageInfo.Text = "Predicting";

            if (bow == null)
            {
                MessageBox.Show("No BoW model!");
                return;
            }
            if (svmIm == null)
            {
                MessageBox.Show("No SVM model!");
                return;
            }

            Bitmap[] trainIms = new Bitmap[imagesEdited.Count];

            ushort z = 0;
            foreach (BitmapImage b in imagesEdited)
                trainIms[z++] = UtilFn.BitmapImage2Bitmap(b);

            double[][] features = bow.Transform(trainIms);

            output = svmIm.Decide(features);
            if (output != null)
                if (output[j] && !cvs.Children.Contains(rectSel2))
                    cvs.Children.Add(rectSel2);
                else if (!output[j] && cvs.Children.Contains(rectSel2))
                    cvs.Children.Remove(rectSel2);
            imageInfo.Text = "Done";
            btnCorrect.IsEnabled = true;

        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            WPFFolderBrowserDialog dd = new WPFFolderBrowserDialog();
            dd.Title = "Select a folder";
            if (dd.ShowDialog() == true)
            {
                if (bow != null)
                    Accord.IO.Serializer.Save<BagOfVisualWords>(bow, dd.FileName + "/bowModel");
                else
                    MessageBox.Show("BoW model does not exist!");
            }
            else
            {
                MessageBox.Show("Something went wrong.");
            }
            imageInfo.Text = "Done";
        }

        private void trainSVMim(object sender, RoutedEventArgs e)
        {
            imageInfo.Text = "Training SVMim!";

            if (bow == null || imagesEdited.Count == 0)
            {
                MessageBox.Show("NO TRAINED BoW MODEL or NO IMAGES LOADED");
                return;
            }
            Bitmap[] trainIms = new Bitmap[imagesEdited.Count];

            ushort z = 0;
            foreach (BitmapImage b in imagesEdited)
                trainIms[z++] = UtilFn.BitmapImage2Bitmap(b);

            int[] labels = new int[imagesEdited.Count];

            for (z = 0; z < imagesEdited.Count; ++z)
                labels[z] = selected[z] ? +1 : -1;

            if (bow == null)
            {
                MessageBox.Show("NO TRAINED BoW MODEL");
                return;
            }

            double[][] features = bow.Transform(trainIms);

            var teacher = new SequentialMinimalOptimization<Linear>()
            {
                Complexity = 70 // make a hard margin SVM
            };

            svmIm = teacher.Learn(features, labels);
            imageInfo.Text = "Done";

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
