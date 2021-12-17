using System;
using System.Collections.Generic;
using System.IO;
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

using RecycleBinFilesRestorer.Classes;

namespace RecycleBinFilesRestorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<DollarPair> dollarListItems;
        private List<ListItem<DollarPair>> filteredList;

        public MainWindow()
        {
            InitializeComponent();
            dollarListItems = new List<DollarPair>();

            txtSourceFiles.Text = "";
            txtSourceFiles.IsReadOnly = true;
            txtFilter.Text = "";
            txtOuptut.Text = "";
            txtOuptut.IsReadOnly = true;
            listOutput.Items.Clear();
            RefreshDollarList();
        }

        private void UpdateStatus(string Status)
        {
            RunGuiInstruction(() =>
            {
                if (labStatus != null)
                {
                    labStatus.Content = Status;
                }
            });
        }

        private void RefreshDollarList()
        {
            string filter = "";
            RunGuiInstruction(() =>
            {
                listPairs.Items.Clear();
                filter = txtFilter.Text;
            });

            bool add = true;
            if (dollarListItems != null)
            {
                //Filter List
                filteredList = new List<ListItem<DollarPair>>();
                var cnt = 0;
                var interval = 10;
                foreach (var li in dollarListItems.OrderBy(d => d.FilePairName))
                {
                    if ((cnt % interval) == 0) UpdateStatus("Filtering List " + cnt + "/" + dollarListItems.Count);
                    var properFileName = li.InfoProperFilePath;
                    var pairname = li.FilePairName;
                    var name = pairname + " - " + properFileName;
                    
                    add = true;

                    if (filter != "" && li.InfoProperFilePath != null && !(li.InfoProperFilePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)) add = false;

                    if (add) filteredList.Add(new ListItem<DollarPair>(name, li));
                    cnt++;
                }

                //Add list
                RunGuiInstruction(() =>
                {
                   foreach (var li in filteredList)
                   {
                       listPairs.Items.Add(li);
                   }
                    labStatus.Content = "Total: " + filteredList.Count;
                });
                UpdateStatus("");
            }
        }

        private void RunGuiInstruction(Action action)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(() => action());
            }
            else
                action();
        }

        private void btnSourceFilesBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                txtSourceFiles.Text = dialog.SelectedPath;
            }

        }

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            var dir = new System.IO.DirectoryInfo(txtSourceFiles.Text);
            if (dir.Exists)
            {
                btnScan.IsEnabled = false;
                Task.Run(() => { ProcessScan(dir); })
                    .ContinueWith((d) => RunGuiInstruction(() => btnScan.IsEnabled = true));
            }
            else
            {
                //Show Error?
            }
        }

        private void ProcessScan(System.IO.DirectoryInfo dir)
        {
            if (!dir.Exists) return;
            UpdateStatus("Getting Directory Listing");
            var fileInfos = dir.GetFiles().ToDictionary(f => f.Name);
            var pairs = new Dictionary<string, DollarPair>();

            var updateInterval = 10;
            var cnt = 0;
            foreach (var f in fileInfos)
            {
                if ((cnt % updateInterval) == 0) UpdateStatus("Processing " + cnt + "/" + fileInfos.Count);
                var fn = f.Value.Name;
                var subType = fn.Substring(0, 2);
                var subName = fn.Substring(2);
                //Has this pair been found
                if (!pairs.ContainsKey(subName))
                {
                    var otherType = "";
                    if (subType.ToUpper() == "$I") otherType = "$R";
                    else otherType = "$I";
                    //Check if we have the $I or $R of it in the list
                    FileInfo? otherFile = fileInfos.GetValueOrDefault(otherType + subName);
                    if (otherFile != null)
                    {
                        var p = new DollarPair();
                        p.SetFileName(f.Value.DirectoryName, subName);
                        pairs.Add(subName, p);
                    }
                }
                cnt++;
            }

            cnt = 0;
            object lk = new object();

            var localListPairs = pairs.Select(d => d.Value).ToList();
            pairs.Clear();
            pairs = null;
            var tot = localListPairs.Count;

            //Get ALLLL the info
            Parallel.ForEach(localListPairs, new ParallelOptions { MaxDegreeOfParallelism = 15 }, (d) =>
            {
                d.GetInfo();
                lock (lk)
                {
                    cnt++;
                    if (cnt % updateInterval == 0) UpdateStatus("Parsing $I Info " + cnt + "/" + tot);
                }

            });
            UpdateStatus("Getting Directory Listing");

            dollarListItems = localListPairs;
            UpdateStatus("");
            RefreshDollarList();
        }

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshDollarList();
        }

        private void btnOutputBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                txtOuptut.Text = dialog.SelectedPath;
            }
            var outDir = txtOuptut.Text;
            Task.Run(() => RefreshOutputs(outDir));
        }

        private string CalcOutput(string intputPath, string outputPath) 
        {
            var fIDir = intputPath.Replace(":", "");
            var finalPath = System.IO.Path.Combine(outputPath, fIDir);
            return finalPath;
        }

        private void RefreshOutputs(string outputLocation)
        {
            if (outputLocation != "" && Directory.Exists(outputLocation))
            {
                RunGuiInstruction(() => listOutput.Items.Clear());
            }

            List<string> outputs = new List<string>(); 
            foreach (var item in filteredList)
            {
                var finalPath = CalcOutput(item.Item.InfoProperFilePath, outputLocation);
                outputs.Add(finalPath);
            }
            RunGuiInstruction(() =>
            {
                foreach (var o in outputs)
                {
                    listOutput.Items.Add(o);
                }
            });
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            btnRun.IsEnabled = false;
            var outDir = txtOuptut.Text;
            var runList = filteredList.Select(d => d.Item).ToList();
            Task.Run(() => RunMoveRename(runList, outDir));
        }

        private void RunMoveRename(List<DollarPair> RunList, string outputLocation)
        {
            var count = 0;
            object lk = new object();

            Parallel.ForEach(RunList, new ParallelOptions { MaxDegreeOfParallelism = 3 }, d =>
            {
                var finalLoc = CalcOutput(d.InfoProperFilePath, outputLocation);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(finalLoc));
                File.Copy(d.DollarRFullPath, finalLoc);
                lock (lk)
                {
                    count++;
                    UpdateStatus("Processing FileRename " + count + "/" + RunList.Count);
                }
            });
            foreach(var pair in RunList)
            {

            }
            UpdateStatus("Done");
        }
    }
}
