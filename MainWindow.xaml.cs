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
                Filter = "Pliki DICOM (*.dcm)|*.dcm|Folder z obrazam

