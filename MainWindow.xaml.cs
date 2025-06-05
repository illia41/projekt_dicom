using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Dicom;
using Dicom.Imaging;
using System.Windows.Media;
using System.Windows;

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
                    try
                    {
                        var dicomFile = DicomFile.Open(file);
                        dicomImages.Add(new DicomImage(dicomFile.Dataset));
                    }
                    catch
                    {
                        MessageBox.Show($"Nie udało się załadować pliku: {file}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // Sort by Z (3rd component of ImagePositionPatient)
                dicomImages.Sort((a, b) =>
                {
                    a.Dataset.TryGetValues(DicomTag.ImagePositionPatient, out double[] aPos);
                    b.Dataset.TryGetValues(DicomTag.ImagePositionPatient, out double[] bPos);
                    return aPos?[2].CompareTo(bPos?[2] ?? 0) ?? 0;
                });

                if (dicomImages.Count == 0)
                {
                    MessageBox.Show("Nie załadowano żadnych prawidłowych obrazów DICOM.");
                    return;
                }

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
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wyświetlania obrazu:\n{ex.Message}");
            }
        }

        enum Plane { Sagittal, Coronal }

        private BitmapSource GetProjectionImage(int sliceIndex, Plane plane)
        {
            int width = dicomImages[0].Width;
            int height = dicomImages[0].Height;
            int depth = dicomImages.Count;

            PixelFormat pixelFormat = PixelFormats.Gray8;
            int bytesPerPixel = (pixelFormat.BitsPerPixel + 7) / 8;

            WriteableBitmap bitmap;
            byte[] pixels;
            int stride;

            switch (plane)
            {
                case Plane.Sagittal:
                    bitmap = new WriteableBitmap(height, depth, 96, 96, pixelFormat, null);
                    pixels = new byte[height * depth];
                    for (int i = 0; i < depth; i++)
                    {
                        var img = dicomImages[i].RenderImage().As<BitmapSource>();
                        byte[] buffer = new byte[width * height];
                        img.CopyPixels(buffer, width, 0);
                        for (int j = 0; j < height; j++)
                        {
                            pixels[i * height + j] = buffer[j * width + sliceIndex];
                        }
                    }
                    stride = height * bytesPerPixel;
                    bitmap.WritePixels(new Int32Rect(0, 0, height, depth), pixels, stride, 0);
                    return bitmap;

                case Plane.Coronal:
                    bitmap = new WriteableBitmap(width, depth, 96, 96, pixelFormat, null);
                    pixels = new byte[width * depth];
                    for (int i = 0; i < depth; i++)
                    {
                        var img = dicomImages[i].RenderImage().As<BitmapSource>();
                        byte[] buffer = new byte[width * height];
                        img.CopyPixels(buffer, width, 0);
                        for (int j = 0; j < width; j++)
                        {
                            pixels[i * width + j] = buffer[sliceIndex * width + j];
                        }
                    }
                    stride = width * bytesPerPixel;
                    bitmap.WritePixels(new Int32Rect(0, 0, width, depth), pixels, stride, 0);
                    return bitmap;
            }

            return null;
        }
    }
}


