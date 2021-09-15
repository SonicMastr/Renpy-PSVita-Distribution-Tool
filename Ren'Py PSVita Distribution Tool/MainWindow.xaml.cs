using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using System.IO.Compression;
using System.Diagnostics;

namespace Ren_Py_PSVita_Distribution_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DirectoryInfo current;
        string gameAssetBase;
        string titleid;
        string appver;
        string appname;
        public MainWindow()
        {
            InitializeComponent();
            current = new DirectoryInfo(Directory.GetCurrentDirectory());
            DirectoryInfo[] directories = current.GetDirectories();
            foreach (DirectoryInfo directory in directories)
            {
                bool isGameDir = false;
                DirectoryInfo[] searchDirs = directory.GetDirectories();
                foreach (DirectoryInfo searchDir in searchDirs)
                {
                    if (searchDir.Name.Equals("game"))
                        isGameDir = true;
                }
                if (isGameDir)
                    gameName.Items.Add(directory.Name);
            }
        }

        private void gameName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Success.Visibility = Visibility.Hidden;
            status.Visibility = Visibility.Hidden;
            gameAssetBase = current.FullName + "\\" + gameName.SelectedItem.ToString() + "\\";
            build_button.IsEnabled = false;
            error.Visibility = Visibility.Visible;
            if (!File.Exists(gameAssetBase + "sce_sys/pic0.png"))
            {
                background.Source = null;
                error.Text = "Missing sce_sys/pic0.png";
                no_preview.Text = "No Preview";
                no_preview.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                no_preview.Visibility = Visibility.Hidden;
                File.Copy(gameAssetBase + "sce_sys/pic0.png", System.IO.Path.GetTempPath() + "\\renpyimage.png", true);
                background.Source = new BitmapImage(new Uri(System.IO.Path.GetTempPath() + "\\renpyimage.png"));
            }
            if (!File.Exists(gameAssetBase + "sce_sys/icon0.png"))
            {
                error.Text = "Missing sce_sys/icon0.png";
                return;
            }
            else if (!File.Exists(gameAssetBase + "sce_sys/livearea/contents/bg.png"))
            {
                error.Text = "Missing sce_sys/livearea/contents/bg.png";
                return;
            }
            else if (!File.Exists(gameAssetBase + "sce_sys/livearea/contents/startup.png"))
            {
                error.Text = "Missing sce_sys/livearea/contents/startup.png";
                return;
            }
            else if (!File.Exists(gameAssetBase + "sce_sys/livearea/contents/template.xml"))
            {
                error.Text = "Missing sce_sys/livearea/contents/template.xml";
                return;
            }
            else
            {
                error.Visibility = Visibility.Hidden;
            }
            UpdateStatus(null, null);
        }

        private void UpdateStatus(object sender, TextChangedEventArgs e)
        {
            Success.Visibility = Visibility.Hidden;
            status.Visibility = Visibility.Hidden;
            if ((background.Source != null) && (error.Visibility == Visibility.Hidden) && (TextBox_TID.Text.Length >= 9) && (TextBox_NAME.Text.Length > 0) && (TextBox_VER.Text.Length >= 5))
            {
                titleid = TextBox_TID.Text.ToUpper();
                appver = TextBox_VER.Text;
                appname = TextBox_NAME.Text;
                build_button.IsEnabled = true;
            }
            else
            {
                build_button.IsEnabled = false;
            }
        }

        // From Microsoft. I'm too damn lazy for this
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = System.IO.Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = System.IO.Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private void build_click(object sender, RoutedEventArgs e)
        {
            string distDirectory = "dists\\" + gameName.SelectedItem.ToString();
            string distFileName = distDirectory + "\\" + appname + "-" + appver + ".zip";
            ProcessStartInfo infoPNG = new ProcessStartInfo();
            infoPNG.WorkingDirectory = Directory.GetCurrentDirectory();
            infoPNG.FileName = "pngquant.exe";
            infoPNG.Arguments = "-f --ext .png " + gameName.SelectedItem.ToString() + "\\sce_sys\\**\\*.png --verbose";
            ProcessStartInfo infomksfo = new ProcessStartInfo();
            infomksfo.WorkingDirectory = Directory.GetCurrentDirectory();
            infomksfo.FileName = "vita-mksfoex.exe";
            infomksfo.Arguments = "-s TITLE_ID=" + titleid + " -d ATTRIBUTE2=12 -s APP_VER=" + appver + " \"" + appname + "\" " + gameName.SelectedItem.ToString() + "\\sce_sys\\param.sfo";
            try
            {
                Success.Visibility = Visibility.Hidden;
                status.Text = "Generating SFO...";
                status.Visibility = Visibility.Visible;
                using (Process exeProcess = Process.Start(infomksfo))
                {
                    exeProcess.WaitForExit();
                }
                status.Text = "Compressing PNG Files...";
                using (Process exeProcess = Process.Start(infoPNG))
                {
                    exeProcess.WaitForExit();
                }
                status.Text = "Copying Required Assets...";
                DirectoryCopy("Assets", gameName.SelectedItem.ToString(), true);
                status.Text = "Creating Output Directory...";
                Directory.CreateDirectory(distDirectory);
                if (File.Exists(distFileName))
                    File.Delete(distFileName);

                status.Text = "Packaging VPK...";
                ZipFile.CreateFromDirectory(gameName.SelectedItem.ToString(), distFileName);
                Success.Text = "Build Successful! Output in dist/" + gameName.SelectedItem.ToString();
                Success.Visibility = Visibility.Visible;
                status.Visibility = Visibility.Hidden;
            }
            catch
            {
                
            }
        }
    }
}
