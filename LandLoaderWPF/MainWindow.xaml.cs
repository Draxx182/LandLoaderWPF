using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using MotionLibrary;
using Newtonsoft.Json;
using System.Text.Json;

namespace LandLoaderWPF
{
    enum PropertyBinLabel
    {
        Exists,
        Extracted,
        Disabled,
        Extracting,
        Repacking
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Variables
        private OpenFolderDialog folderDialog = new OpenFolderDialog();
        private PropertyBinLabel propertyChecker = new PropertyBinLabel();

        public string DialogPath;
        public string FolderName;
        public string PropertyName;

        public MainWindow()
        {
            InitializeComponent();

            // Setting up Property.Bin Checker
            propertyChecker = PropertyBinLabel.Disabled;
            SetForProperty();

            // Open Folder Dialog - https://stackoverflow.com/questions/7921672/how-to-add-comments-into-a-xaml-file-in-wpf
            folderDialog.Title = "Open Motion Path";
            /*
            folderDialog.IsFolderPicker = true;

            folderDialog.AddToMostRecentlyUsedList = false;
            folderDialog.AllowNonFileSystemItems = false;
            folderDialog.EnsureFileExists = true;
            folderDialog.EnsurePathExists = true;
            folderDialog.EnsureReadOnly = false;
            folderDialog.EnsureValidNames = true;
            folderDialog.Multiselect = false;
            folderDialog.ShowPlacesList = true; */
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            // Continued from Open Folder Dialog
            if (folderDialog.ShowDialog() == true)
            {
                var folder = folderDialog.FolderName;
                MotionName.Text = folder;
                CheckForProperty(folder);
            }
        }

        private void CheckForProperty(string path)
        {
            DialogPath = path;
            PropertyName = System.IO.Path.Combine(DialogPath, ConfigurationManager.AppSettings.Get("PropertyBinName"));
            FolderName = System.IO.Path.Combine(DialogPath, ConfigurationManager.AppSettings.Get("PropertyBinFolder"));

            // If the extracted property.bin folder exists:
            if (Directory.Exists(FolderName))
            {
                propertyChecker = PropertyBinLabel.Extracted;
                SetForProperty();
                return;
            } // If property.bin exists but is not extracted
            else if (File.Exists(PropertyName))
            {
                propertyChecker = PropertyBinLabel.Exists;
                SetForProperty();
                return;
            }

            // Returns Not Found if file paths don't exist.
            propertyChecker = PropertyBinLabel.Disabled;
            SetForProperty();
            return;
        }

        /// <summary>
        /// Sets the label.
        /// </summary>
        private void SetForProperty()
        {
            switch (propertyChecker)
            {
                case PropertyBinLabel.Extracted:
                    PropertyCheckLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF07B119"));
                    PropertyCheckLabel.Content = "Extracted";
                    break;
                case PropertyBinLabel.Exists:
                    PropertyCheckLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB91313"));
                    PropertyCheckLabel.Content = "Not Extracted";
                    break;
                case PropertyBinLabel.Disabled:
                    PropertyCheckLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3957A2"));
                    PropertyCheckLabel.Content = "Not Found";
                    break;
                case PropertyBinLabel.Extracting:
                    PropertyCheckLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3957A2"));
                    PropertyCheckLabel.Content = "Extracting";
                    break;
            }
        }

        /// <summary>
        /// Checks the progress of the current property.bin.
        /// </summary>
        private void ExtractButton_Click(object sender, RoutedEventArgs e)
        {
            switch (propertyChecker)
            {
                case PropertyBinLabel.Disabled:
                    MessageBox.Show("The current Motion Path is not valid/property.bin cannot be located in this directory. " +
                            "Please ensure that you are pointing to the a 'motion' folder, or motion_w64.",
                            "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case PropertyBinLabel.Extracted:
                    var result = MessageBox.Show("Are you sure you want to want to re-extract property.bin? This will overwrite moves listed in the current property.bin.", "Extraction Confirmation",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                        ProcessExtraction();
                    break;
                case PropertyBinLabel.Exists:
                    ProcessExtraction();
                    break;
                case PropertyBinLabel.Extracting:
                    MessageBox.Show("Please wait for the extraction to finish.", "Extraction In Progress",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        private void ProcessExtraction()
        {
            propertyChecker = PropertyBinLabel.Extracting;
            SetForProperty();

            // Attempts to read the property.bin and stores the moves read.
            OldEngineFormat propertyBin = OldEngineFormat.Read(PropertyName);
            List<OEAnimEntry> movesRead = propertyBin.Moves;
                
            Directory.CreateDirectory(FolderName);
            string movesFolder = System.IO.Path.Combine(FolderName, ConfigurationManager.AppSettings.Get("MovesFolder"));
            string actionFolder = System.IO.Path.Combine(FolderName, ConfigurationManager.AppSettings.Get("ActionSetFolder"));
            //string fileInformation = System.IO.Path.Combine(FolderName, ConfigurationManager.AppSettings.Get("FileInformation"));
            Directory.CreateDirectory(movesFolder);
            Directory.CreateDirectory(actionFolder);

            string testName = movesRead[5309].Name;
            //string testSerialization = SerializeOEAnimEntry(movesRead[5309]);
            string testPath = ConcatFileName(movesFolder, testName + ".json");
            WriteToJsonFile(testPath, movesRead[5309]);

            //Directory.CreateDirectory(fileInformation);
            if (Directory.Exists(FolderName))
            {
                propertyChecker = PropertyBinLabel.Extracted;
                SetForProperty();
                return;
            }
            else
            {
                MessageBox.Show("There was an error extracting property.bin. Please ensure that your file is not corrupted.",
                                "Extraction Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                propertyChecker = PropertyBinLabel.Exists;
                SetForProperty();
                return;
            }
        }

        /// <summary>
        /// Returns a concatenated file path and a file name.
        /// </summary>
        private string ConcatFileName(string path, string name){
            return System.IO.Path.Combine(path, name);
        }

        /// <summary>
        /// Write a JSON file using a given object.
        /// </summary>
        /// <param name="filePath">File path to write to.</param>
        /// <param name="objectToSerialize">Object to serialize.</param>
        private void WriteToJsonFile(string filePath, params object[] objectsToSerialize)
        {
            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            // Creates the stream and the JsonWriter to serialize a new file.
            using (StreamWriter wr = new StreamWriter(filePath))
            {
                using (JsonWriter writer = new JsonTextWriter(wr))
                {
                    writer.Formatting = Formatting.Indented;
                    foreach (Object obj in objectsToSerialize) { 
                        serializer.Serialize(writer, obj);
                    }
                }
            }
        }

        private void RepackButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Repacking is unavailable.");
        }

        private void PropertyEditorMenuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            TabItem newTabItem = new TabItem
            {
                Header = "Test LONGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG",
                Name = "Test"
            };
            this.PropertyEditorTabControl.Items.Add(newTabItem);
        }
    }
}
