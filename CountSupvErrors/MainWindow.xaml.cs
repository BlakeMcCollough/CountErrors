using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;

namespace CountSupvErrors
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _cancelled; //true if cancel button is clicked
        private int _fileCounter; //counts total files
        private int _filesToRead; //total number of files
        private long _startTime;
        private long _endTime;
        private List<SupvError> _listOfErrors; //lists ALL errors found in ALL folders
        private Dictionary<string, int> _totalErrorCount; //keeps track of the amount of errors found for each code
        private DateTime? _startDate;
        private DateTime? _endDate;
        private RegexConsts _regex; //all my regexes, see RegexConsts.cs
        private BackgroundWorker _worker; //handles multithreaded UI-engine communication
        public MainWindow()
        {
            InitializeComponent();
            InstantiateWorker();
            _fileCounter = 0;
            _listOfErrors = new List<SupvError>();
            _totalErrorCount = new Dictionary<string, int>();
            _regex = new RegexConsts();
            
        }

        private void InstantiateWorker()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += backgroundDoWork;
            _worker.ProgressChanged += backgroundProgress;
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.RunWorkerCompleted += backgroundFinished;
        }

        //is called by UI main thread and begins the read process
        private void backgroundDoWork(object sender, DoWorkEventArgs e)
        {
            string rootFolder = e.Argument.ToString();
            if (Directory.Exists(rootFolder) == true)
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
            if (_cancelled == true)
            {
                _cancelled = false;
                _fileCounter = 0;
                App.Cursor = System.Windows.Input.Cursors.Arrow;
                LoadView.Visibility = Visibility.Hidden;
                ParseSettings.Visibility = Visibility.Visible;
                InstantiateWorker();
                return;
            }
            DisplayResultList();
            LoadView.Visibility = Visibility.Hidden;
            ResultView.Visibility = Visibility.Visible;
            _endTime = DateTimeOffset.Now.Ticks / TimeSpan.TicksPerMillisecond;
            //HeaderLabel.Content = "Read through " + _fileCounter.ToString() + " files in " + (_endTime - _startTime).ToString() + "(ms)";
            App.Cursor = System.Windows.Input.Cursors.Arrow;
            _fileCounter = 0;
        }

        //opens a file, reads contents, adds to dictionary if a properly formatted error message is read WARNING: May print weird things if in improper format
        private void CountInFile(string path, string parent)
        {
            StreamReader infile = new StreamReader(path);
            string line = infile.ReadLine();
            while (line != null)
            {
                if (_regex.ErrorLineFound.IsMatch(line) == true)
                {
                    string error = line.Substring(line.IndexOf("C2A00"), 7);
                    _listOfErrors.Add(new SupvError() { ErrorCode = error, Client = parent });
                    if (_totalErrorCount.ContainsKey(error) == true)
                    {
                        _totalErrorCount[error] = _totalErrorCount[error] + 1;
                    }
                    else
                    {
                        _totalErrorCount.Add(error, 1);
                    }
                }
                line = infile.ReadLine();
            }
            infile.Close();
        }

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
            _listOfErrors.Clear();
            _totalErrorCount.Clear();
            LoadingText.Content = "";
            ProgBar.Value = 0;
            App.Cursor = System.Windows.Input.Cursors.Wait;
            _startDate = StartDate.SelectedDate;
            _endDate = EndDate.SelectedDate;
            _startTime = DateTimeOffset.Now.Ticks / TimeSpan.TicksPerMillisecond;
            _worker.RunWorkerAsync(RootBox.Text);
        }

        //Opens all files in a given dir, calls self for all inner directories
        private void RecursiveLookInDir(string path)
        {
            //Console.WriteLine("Opening new directory at " + path);
            if (Directory.Exists(path) == false)
            {
                return;
            }

            foreach (string file in Directory.GetFiles(path))
            {
                if (_regex.ProcessedFileAccept.IsMatch(Path.GetFileName(file)) && IsWithinDateRange(Path.GetFileNameWithoutExtension(file)))
                {
                    CountInFile(file, Path.GetFileName(Path.GetDirectoryName(path)));
                }
                _fileCounter = _fileCounter + 1;
                _worker.ReportProgress((int)(100 * ((double)_fileCounter / (double)_filesToRead)), "Reading from " + (Path.GetFileName(Path.GetDirectoryName(path))));
            }


            foreach (string dir in Directory.GetDirectories(path))
            {
                RecursiveLookInDir(dir);
            }
        }

        private bool IsWithinDateRange(string filename)
        {
            DateTime filetime = DateTime.ParseExact(filename.Substring(8, 13), "yyMMdd.HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            if (_startDate == null && _endDate == null)
            {
                return true;
            }
            else if (_startDate == null && filetime.CompareTo(((DateTime)_endDate).AddSeconds(86399)) <= 0)
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

        private void DisplayResultList()
        {
            var query = _listOfErrors.GroupBy(
                err => new { err.ErrorCode, err.Client }, //we're grouping by errorcode AND client, so all duplicates in this list will be grouped
                (baseErr, clients) => new { Error = baseErr.ErrorCode + " (" + _totalErrorCount[baseErr.ErrorCode].ToString() + ")",
                                            Client = baseErr.Client, Count = clients.Count(), Total = _totalErrorCount[baseErr.ErrorCode] }); //baseErr is the resulting group's key, clients is the list of items counted

            //makes a new XAML groupdescription that will group everything by the common error codes
            ResultList.ItemsSource = query;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(ResultList.ItemsSource);
            view.GroupDescriptions.Add(new PropertyGroupDescription("Error"));
            view.SortDescriptions.Add(new SortDescription("Total", ListSortDirection.Descending));
            view.SortDescriptions.Add(new SortDescription("Count", ListSortDirection.Descending));
            _listOfErrors.Clear();
            _totalErrorCount.Clear();
        }


        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            ResultView.Visibility = Visibility.Hidden;
            ParseSettings.Visibility = Visibility.Visible;
            ResultList.ItemsSource = null;
        }
        
        private string FormatTableToString()
        {
            string outString = "";
            foreach(var item in ResultList.Items)
            {
                outString = string.Concat(outString, item);
                outString = string.Concat(outString, "\r\n");
            }
            return outString;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileWindow = new SaveFileDialog();
            saveFileWindow.Filter = "Text File (*.txt)|*.txt";
            saveFileWindow.Title = "Save to";
            saveFileWindow.ShowDialog();
            if(string.IsNullOrWhiteSpace(saveFileWindow.FileName) == false)
            {
                try
                {
                    File.WriteAllText(saveFileWindow.FileName, FormatTableToString());
                }
                catch (IOException exe)
                {
                    System.Windows.MessageBox.Show(exe.Message);
                }
            }
        }

        private void RootBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(RootBox.Text) == false)
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
