using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Dicom;
using Dicom.Imaging;

namespace DicomViewer
{
    public partial class MainWindow : Window
    {
        private List<DicomImage> dicomImages = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadDicom_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Pliki DICOM (*.dcm)|*.dcm|Folder z obrazami DICOM|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                dicomImages.Clear();

                foreach (var file in dialog.FileNames)
                {
                    var dicomFile = DicomFile.Open(file);
                    dicomImages.Add(new DicomImage(dicomFile.Dataset));
                }

                dicomImages.Sort((a, b) => a.Dataset.GetSingleValue<double>(DicomTag.ImagePositionPatient, 2)
                    .CompareTo(b.Dataset.GetSingleValue<double>(DicomTag.ImagePositionPatient, 2)));

                SliceSlider.Maximum = dicomImages.Count - 1;
                SliceSlider.Value = 0;

                ShowImages(0);
            }
        }

        private void SliceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ShowImages((int)SliceSlider.Value);
        }

        private void ShowImages(int index)
        {
            if (dicomImages.Count == 0 || index >= dicomImages.Count)
                return;

            try
            {
                var axial = dicomImages[index].RenderImage().As<BitmapSource>();
                ImageAxial.Source = axial;

                var sagittal = GetProjectionImage(index, Plane.Sagittal);
                var coronal = GetProjectionImage(index, Plane.Coronal);

                ImageSagittal.Source = sagittal;
                ImageCoronal.Source = coronal;
            }
            catch
            {
                MessageBox.Show("Błąd podczas wyświetlania obrazu.");
            }
        }

        enum Plane { Sagittal, Coronal }

        private BitmapSource GetProjectionImage(int sliceIndex, Plane plane)
        {
            int width = dicomImages[0].Width;
            int height = dicomImages[0].Height;
            int depth = dicomImages.Count;

            WriteableBitmap bitmap;
            int stride, i, j;

            switch (plane)
            {
                case Plane.Sagittal:
                    bitmap = new WriteableBitmap(height, depth, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);
                    stride = bitmap.PixelWidth;
                    byte[] pixelsS = new byte[height * depth];
                    for (i = 0; i < depth; i++)
                    {
                        var img = dicomImages[i].RenderImage().As<BitmapSource>();
                        byte[] buffer = new byte[width * height];
                        img.CopyPixels(buffer, width, 0);
                        for (j = 0; j < height; j++)
                        {
                            pixelsS[i * height + j] = buffer[j * width + sliceIndex];
                        }
                    }
                    bitmap.WritePixels(new Int32Rect(0, 0, height, depth), pixelsS, stride, 0);
                    return bitmap;

                case Plane.Coronal:
                    bitmap = new WriteableBitmap(width, depth, 96, 96, System.Windows.Media.PixelFormats.Gray8, null);
                    stride = bitmap.PixelWidth;
                    byte[] pixelsC = new byte[width * depth];
                    for (i = 0; i < depth; i++)
                    {
                        var img = dicomImages[i].RenderImage().As<BitmapSource>();
                        byte[] buffer = new byte[width * height];
                        img.CopyPixels(buffer, width, 0);
                        for (j = 0; j < width; j++)
                        {
                            pixelsC[i * width + j] = buffer[sliceIndex * width + j];
                        }
                    }
                    bitmap.WritePixels(new Int32Rect(0, 0, width, depth), pixelsC, stride, 0);
                    return bitmap;
            }

            return null;
        }
    }
}
