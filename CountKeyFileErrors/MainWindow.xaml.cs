using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

namespace CountKeyFileErrors
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _cancelled; //true if cancel button is clicked
        private int _fileCounter; //counts total files
        private int _errorMin; //must be this number or largest to show up in the txt doc
        private int _filesToRead; //total number of files
        private long _startTime;
        private long _endTime;
        private string _megaText; //everything being read is stored into megatext, which will be written to the txt doc in the end
        private string _outputPath; //path to the output (duh)
        private Dictionary<(string, string), int> _keyErrorCount; //stores errors for each customer as Customer<(KeyFile, ErrorNo), Count>
        private DateTime? _startDate;
        private DateTime? _endDate;
        private RegexConsts _regex; //all my regexes, see RegexConsts.cs
        private BackgroundWorker _worker; //handles multithreaded UI-engine communication
        public MainWindow()
        {
            InitializeComponent();
            InstantiateWorker();
            _fileCounter = 0;
            _keyErrorCount = new Dictionary<(string, string), int>();
            _regex = new RegexConsts();
            _outputPath = "";
            _errorMin = 10;
        }

        //creates the new background worker object and sets appropriate events
        private void InstantiateWorker()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += backgroundDoWork;
            _worker.ProgressChanged += backgroundProgress;
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerCompleted += backgroundFinished;
        }

        //is called by UI main thread and begins the read process, e.Argument is the root folder's path
        private void backgroundDoWork(object sender, DoWorkEventArgs e)
        {
            string rootFolder = e.Argument.ToString();
            if(Directory.Exists(rootFolder) == true)
            {
                _filesToRead = Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories).Length;
                RecursiveLookInDir(rootFolder);
            }
        }
        //is called when .ReportProgress is called by internal DoWork
        private void backgroundProgress(object sender, ProgressChangedEventArgs e)
        {
            ProgBar.Value = (int)e.ProgressPercentage;
            LoadingText.Content = e.UserState.ToString();
        }
        //when reading files are finished, this is called
        private void backgroundFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            if(_cancelled == true)
            {
                _cancelled = false;
                _fileCounter = 0;
                _megaText = "";
                App.Cursor = System.Windows.Input.Cursors.Arrow;
                LoadView.Visibility = Visibility.Hidden;
                ParseSettings.Visibility = Visibility.Visible;
                InstantiateWorker();
                return;
            }
            LoadView.Visibility = Visibility.Hidden;
            ResultView.Visibility = Visibility.Visible;
            _endTime = DateTimeOffset.Now.Ticks / TimeSpan.TicksPerMillisecond;
            DumpContentsToFile();
            HeaderLabel.Content = "Read through " + _fileCounter.ToString() + " files in " + (_endTime - _startTime).ToString() + "(ms)";
            if (String.IsNullOrWhiteSpace(_megaText) == true)
            {
                FileNameLabel.Content = "No errors founds";
                Opener.IsEnabled = false;
            }
            else
            {
                FileNameLabel.Content = "Saved to " + _outputPath;
                Opener.IsEnabled = true;
            }
            App.Cursor = System.Windows.Input.Cursors.Arrow;
            _fileCounter = 0;
            _megaText = "";
        }

        //opens a file, reads contents, adds to dictionary if a properly formatted error message is read WARNING: May print weird things if in improper format
        private void CountInFile(string path)
        {
            StreamReader infile = new StreamReader(path);
            string line = infile.ReadLine();
            string keyFile = "";
            string errorNo = "";
            while(line != null)
            {
                if(_regex.KeyLineFound.IsMatch(line) == true || _regex.KeyLineFound6789.IsMatch(line) == true)
                {
                    keyFile = line.Substring(line.LastIndexOf('|')+2, 42); //42 is just padding
                    keyFile = keyFile.TrimEnd();
                }
                else if(String.IsNullOrEmpty(keyFile) == false && (_regex.ErrorLineFound.IsMatch(line) == true || _regex.ErrorLineFound6789.IsMatch(line) == true))
                {
                    errorNo = "8" + (line[line.IndexOf("supv: 8")+7]).ToString();
                    if(_keyErrorCount.ContainsKey((keyFile, errorNo)) == true)
                    {
                        _keyErrorCount[(keyFile, errorNo)] = _keyErrorCount[(keyFile, errorNo)] + 1;
                    }
                    else
                    {
                        _keyErrorCount.Add((keyFile, errorNo), 1);
                    }
                    keyFile = "";
                    errorNo = "";
                }
                line = infile.ReadLine();
            }
            infile.Close();
        }

        //browse button is clicked displaying WinForm folder browser
        private void ChangeRoot(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog newFolderWindow = new FolderBrowserDialog();
            newFolderWindow.ShowNewFolderButton = false;
            newFolderWindow.Description = "Choose an existing folder";
            newFolderWindow.RootFolder = Environment.SpecialFolder.MyComputer;
            newFolderWindow.SelectedPath = RootBox.Text; //\\qau3\Customers\
            newFolderWindow.ShowDialog();
            RootBox.Text = newFolderWindow.SelectedPath;
        }

        //Button clicked, start reading files
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ParseSettings.Visibility = Visibility.Hidden; //show loading screen
            LoadView.Visibility = Visibility.Visible;
            LoadingText.Content = "";
            ProgBar.Value = 0;
            App.Cursor = System.Windows.Input.Cursors.Wait;
            _megaText = "";
            _startDate = StartDate.SelectedDate;
            _endDate = EndDate.SelectedDate;
            _startTime = DateTimeOffset.Now.Ticks / TimeSpan.TicksPerMillisecond;
            _worker.RunWorkerAsync(RootBox.Text);
        }

        //Opens all files in a given dir, calls self for all inner directories
        private void RecursiveLookInDir(string path)
        {
            //Console.WriteLine("Opening new directory at " + path);
            if(Directory.Exists(path) == false)
            {
                return;
            }
            _keyErrorCount.Clear();
            
            foreach(string file in Directory.GetFiles(path))
            {
                if(_regex.ProcessedFileAccept.IsMatch(Path.GetFileName(file)) && IsWithinDateRange(Path.GetFileNameWithoutExtension(file)))
                {
                    CountInFile(file);
                }
                _fileCounter = _fileCounter + 1;
                _worker.ReportProgress((int)(100 * ((double)_fileCounter / (double)_filesToRead)),
                    "Reading from " + (Path.GetFileName(Path.GetDirectoryName(path))));
            }

            //this directory is SUPV, its parent is a customer, and some errors exist
            _keyErrorCount = _keyErrorCount.Where(x => x.Value >= _errorMin).ToDictionary(x => x.Key, x => x.Value);
            if (String.Equals(Path.GetFileName(path), "SUPV", StringComparison.CurrentCultureIgnoreCase) == true && _regex.CustomerNameAccept.IsMatch((Path.GetFileName(Path.GetDirectoryName(path)))) && _keyErrorCount.Count > 0) //String.Equals(Path.GetFileName(path), "SUPV", StringComparison.CurrentCultureIgnoreCase) == true && 
            {
                _megaText = _megaText + Path.GetFileName(Path.GetDirectoryName(path));
                foreach(KeyValuePair<(string, string), int> item in _keyErrorCount)
                {
                    _megaText = _megaText + " " + item.Key.Item1 + " " + item.Key.Item2 + " " + item.Value.ToString();
                }
                _megaText = _megaText + "\r\n";
            }


            foreach (string dir in Directory.GetDirectories(path))
            {
                RecursiveLookInDir(dir);
            }
        }

        //checks if the given filename's timestamp is inbetween the correct dates
        private bool IsWithinDateRange(string filename)
        {
            DateTime filetime = DateTime.ParseExact(filename.Substring(8, 13), "yyMMdd.HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            if (_startDate == null && _endDate == null)
            {
                return true;
            }
            else if(_startDate == null && filetime.CompareTo(((DateTime)_endDate).AddSeconds(86399)) <= 0)
            {
                return true;
            }
            else if (_endDate == null && filetime.CompareTo(_startDate) >= 0)
            {
                return true;
            }
            else if (_startDate != null && _endDate != null && filetime.CompareTo(((DateTime)_endDate).AddSeconds(86399)) <= 0 && filetime.CompareTo(_startDate) >= 0)
            {
                return true;
            }
            return false;

        }

        //writes everything from _megaText to a file saved in same directory as .exe
        private void DumpContentsToFile()
        {
            if(String.IsNullOrWhiteSpace(_megaText) == true) //no contents to dump
            {
                return;
            }
            
            _outputPath = "Diagnosis.txt";
            int i = 1;
            while(File.Exists(_outputPath) == true)
            {
                i = i + 1;
                _outputPath = $"Diagnosis ({i}).txt";
            }
            StreamWriter outfile = new StreamWriter(_outputPath);
            outfile.WriteLine(_megaText);
            outfile.Close();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if(String.IsNullOrEmpty(_outputPath) == true)
            {
                Console.WriteLine("An error occured");
                return;
            }
            Process.Start(_outputPath);
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            ResultView.Visibility = Visibility.Hidden;
            ParseSettings.Visibility = Visibility.Visible;
        }

        private void SkipThreshold_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                _errorMin = Int32.Parse(SkipThreshold.Text);
            }
            catch
            {
                SkipThreshold.Text = "10";
                _errorMin = 10;
            }
        }

        private void RootBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(Directory.Exists(RootBox.Text) == false)
            {
                RootBox.Text = @"D:\QEServer\Customers\";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelled = true;
            _worker = null;
        }
    }
}
