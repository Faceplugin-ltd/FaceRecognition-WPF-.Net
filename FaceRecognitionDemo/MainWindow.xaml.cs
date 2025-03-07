using FaceSDK;
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;


namespace FaceRecognitionDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    string firstImagePath;
    string secondImagePath;
    string findImagePath;
    double matchingThreshold = 0.7;

    FaceEngineClass faceSDK = new FaceEngineClass();
    public MainWindow()
    {
        InitializeComponent();
        var dictPath = $"{AppDomain.CurrentDomain.BaseDirectory}assets";
        int ret = faceSDK.Init(dictPath);
        if (ret != (int)SDK_STATUS.SDK_SUCCESS)
        {
            System.Windows.MessageBox.Show("SDK Init Failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        FaceTemplateDB.CreateTable();
        ActivateSDK();
    }

    private void ActivateSDK()
    {
        string licenseFile = "license.txt";
        string license = null;
        int ret = -1;
        TextboxHWID.Text = faceSDK.GetHardwareId();
        try
        {
            license = File.ReadAllText(licenseFile);
            //System.Windows.MessageBox.Show(license, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("License File Read Error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        //System.Windows.MessageBox.Show(faceSDK.GetHardwareId(), "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        
        try
        {
             ret = faceSDK.Activate(license);
        }
        catch
        {

        }

        if (ret == 0)
        {
            LabelActivation.Content = "Activated";
        }
        else
        {
            LabelActivation.Content = "Not Activated";
        }

    }
    private void Label1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select an Image",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            firstImagePath = openFileDialog.FileName;
            FaceImage1.Source = new BitmapImage(new Uri(firstImagePath));
        }
    }

    private void Label2_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select an Image",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            secondImagePath = openFileDialog.FileName;
            FaceImage2.Source = new BitmapImage(new Uri(secondImagePath));
        }
    }

    private void Label3_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select a face image to find",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            findImagePath = openFileDialog.FileName;
            FaceImage3.Source = new BitmapImage(new Uri(findImagePath));
        }
    }

    private void BtnCompare_Click(object sender, RoutedEventArgs e)
    {

        string isSamePerson;
        float similarity;
        float[] feature1 = new float[128];
        float[] feature2 = new float[128];

        if (firstImagePath == null || secondImagePath == null)
        {
            System.Windows.MessageBox.Show("Please select two face images to compare", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var (pixels1, width1, height1, stride1) = ImageProcessor.ProcessImage(firstImagePath);
        var (pixels2, width2, height2, stride2) = ImageProcessor.ProcessImage(secondImagePath);

        int ret1 = faceSDK.Extract(pixels1, width1, height1, stride1, feature1);
        int ret2 = faceSDK.Extract(pixels2, width2, height2, stride2, feature2);

        similarity = faceSDK.Similarity(feature1, feature2);

        string strSimilarity = "similarity: " + similarity.ToString();

        if (similarity > matchingThreshold)
        {
            isSamePerson = "Same Person\n";

        }
        else
        {
            isSamePerson = "Different Person\n";
        }
        TextResult.Text = "Result\n" + isSamePerson + strSimilarity;
    }

    private void BtnEnroll_Click(object sender, RoutedEventArgs e)
    {
        string selectedFolder = null;
        string[] imageFiles;
        using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
        {
            folderDialog.Description = "Select a folder to enroll all the face images in it";
            //folderDialog.ShowNewFolderButton = true;

            DialogResult result = folderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            {
                selectedFolder = folderDialog.SelectedPath;
                //System.Windows.MessageBox.Show("Selected Folder: " + selectedFolder);
            }
        }

        // Define the image file extensions you want to process
        string[] imageExtensions = { "*.jpg", "*.jpeg", "*.png" };

        // Get all image files in the folder (using a wildcard search)
        foreach (var extension in imageExtensions)
        {
            // Get the files that match the extension
            imageFiles = Directory.GetFiles(selectedFolder, extension);

            // Iterate through the image files
            foreach (string fileName in imageFiles)
            {
                //System.Windows.MessageBox.Show("Selected file: " + fileName);
                float[] feature = new float[128];
                var (pixels, width, height, stride) = ImageProcessor.ProcessImage(fileName);
                int ret1 = faceSDK.Extract(pixels, width, height, stride, feature);
                FaceTemplateDB.StoreFaceTemplate(fileName, feature);
            }
        }

        System.Windows.MessageBox.Show("Enrollment Done!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void BtnIdentify_Click(object sender, RoutedEventArgs e)
    {

        bool isMatchFound = false;
        float[] targetFeature = new float[128];
        float similarity;

        if (findImagePath == null)
        {
            System.Windows.MessageBox.Show("Please select a face image to find", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var (pixels, width, height, stride) = ImageProcessor.ProcessImage(findImagePath);
        int ret1 = faceSDK.Extract(pixels, width, height, stride, targetFeature);

        foreach (int i in Enumerable.Range(1, FaceTemplateDB.GetHighestId()))
        {
            float[] tempFeature = FaceTemplateDB.GetFaceTemplateById(i);
            similarity = faceSDK.Similarity(targetFeature, tempFeature);

            if (similarity > matchingThreshold)
            {
                isMatchFound = true;
                string fileName = FaceTemplateDB.GetUsernameById(i);
                System.Windows.MessageBox.Show("Matched Image File name:" + fileName, "Match Found", MessageBoxButton.OK, MessageBoxImage.Information);
                Label4.Content = fileName;
                FaceImage4.Source = new BitmapImage(new Uri(fileName));
            }
        }

        if (!isMatchFound)
        {
            System.Windows.MessageBox.Show("No Match Found in the DB.", "No Match Found", MessageBoxButton.OK, MessageBoxImage.Information);
            FaceImage4.Source = null;
            Label4.Content = "";
        }
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        FaceTemplateDB.DeleteAllData();
        System.Windows.MessageBox.Show("Face Template DB cleared", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        FaceImage4.Source = null;
        Label4.Content = "";
    }
}