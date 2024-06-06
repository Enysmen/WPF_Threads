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
using System.Text.RegularExpressions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using System.Data.OleDb;

namespace WpfApp.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    // creating a GetDataView class for the ObservableCollection 
    public class GetDataVIew 
    {
        public int Threadid { get; set; }

        public int Coreid { get; set; }

        public string? _FinalString { get; set; }
    }

   
    public partial class MainWindow : Window
    {
        private ObservableCollection<GetDataVIew> Data {get; set;} = new ObservableCollection<GetDataVIew>(); // creates a Data collection that contains objects of type GetDataVIew

        private CancellationTokenSource cts = new CancellationTokenSource(); // token creation

        private string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; // creating chars to generate a string

        Random random = new Random();


        public MainWindow()
        {
            InitializeComponent();

            ListViewThread.ItemsSource = Data; // assignment Data collection for ListViewThread

            Log.Logger = new LoggerConfiguration().WriteTo.File("E:\\C# uroki\\WpfApp.NET\\Logs\\log.txt").CreateLogger(); // create logger (Serilog) , Before launching, please provide your path to the log.txt file 

        }

        // block code check input ----

        private void EntryNumberUser(object sender, TextChangedEventArgs e)
        {
           

        }
        //PreviewKeyDown
        private void TextUser_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // prevent space from being entered
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        // PreviewTextInput 
        private void NumberValidationTextUser(object sender, TextCompositionEventArgs e)
        {
            // prevents the user from entering characters
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // PreviewExecuted check copypaste through CTRL + V 
        private void TextUser_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }

        }
        // method to check if the user is entering a valid range and also Null input
        private bool chechInputRange ()
        {
            bool BoolAsnwChek = false;

            if (string.IsNullOrWhiteSpace(TextUser.Text))
            {
                MessageBox.Show("Please enter a value.", "Warning");
                return false;
            }

            uint checkRange = uint.Parse(TextUser.Text);

            if (checkRange >= 2 && checkRange <= 15) 
            {
                BoolAsnwChek = true;
            }
            else
            {
                MessageBox.Show("I'm so sorry but I won't work like this anymore,Pls enter the correct range", "Warning");
            }

            return BoolAsnwChek;
        }
        // block code check input ---




        // The code block is responsible for generating a string, creating threads, outputting a string, writing data to the database, stopping threads -- 
        // method to create a new thread 
        private void StartThreads(object sender, RoutedEventArgs e)
        {
            if (chechInputRange())
            {
                uint numbersOfThreads = uint.Parse(TextUser.Text);
                cts = new CancellationTokenSource(); // creating a re-token to start threads again after they are stopped

                // creating threads 
                for (int i = 0; i < numbersOfThreads; i++)
                {
                    /*
                     * Using async inside thread creation: Using async inside a lambda expression passed to the thread constructor is not good practice
                     * This can lead to unpredictable behavior and difficulty tracking down errors.
                     * Thread does not wait for an asynchronous operation to complete and may complete before the asynchronous operation completes.
                     */
                    Thread th = new Thread(async () => 
                    {
                        await Task.Run(() => GenerateRandomString(cts.Token)); 
                    });

                    th.Name = "GenerateString"; // I give the name of the stream to make it easier to understand what kind of streams they are in debugging mode
                    th.Start(); // start Threads
                }

                SortThreadID(); 
            }

        }
        // the method is responsible for sorting in ListViewThread sorting is done over the ThreadID field
        private void SortThreadID()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(ListViewThread.ItemsSource);

            view.SortDescriptions.Add(new SortDescription("Threadid", ListSortDirection.Ascending));
        }
        //Button StopThreads
        private void StopThreads(object sender, RoutedEventArgs e)
        {
            cts.Cancel(); // we call the Cancel method to send a request to shut down threads, this is safer than using the Abort method 
        }

        // a method that is called by a new thread to generate a string of length 5-10
        private void GenerateRandomString (CancellationToken cancellationToken)
        {
            int stringLength = random.Next(5, 11); // create a length for the string
            char[] stringChars = new char[stringLength]; // create an array type char
            int thid = Thread.CurrentThread.ManagedThreadId; // We also create a variable that will store the ID of the current thread
            int coreid = Thread.GetCurrentProcessorId(); // and get the id of the core on which the current thread is running

            while (!cancellationToken.IsCancellationRequested)  // check if the thread has received a shutdown request
            {
                //GenerateString
                Thread.Sleep(random.Next(500, 2000));
                for (int i = 0; i < stringLength; i++)
                {
                    stringChars[i] = chars[random.Next(chars.Length)];
                }

                string finalString = new String(stringChars);

                AddToListView(finalString, thid, coreid);
                _ = AddDataToSQL(finalString, thid, coreid);

            } 

        }
        // method that is responsible for adding elements to the ListViewThread list
        private void AddToListView(string FinalString,int thid,int coreid)
        {
                // we guarantee that the operation will be performed on the main thread if the current thread stops
                Dispatcher.Invoke(() =>  
                {
                    Data.Add(new GetDataVIew { Coreid = coreid, Threadid = thid, _FinalString = FinalString });

                    if (Data.Count >= 20)
                    {
                        Data.RemoveAt(0);
                    }
                });
        }
        // asynchronous method adding data to the database
        private async Task AddDataToSQL(string flstring, int thid, int coreid)
        {
            string query = $"INSERT INTO ThreadInfo (Coreid,ThreadID,CreationDate,GenerateString) VALUES (@Corei,@ThreadID,@CreationDate,@GenerateString)";  
            string oldbconnection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=E:\\C# uroki\\WpfApp.NET\\bin\\Debug\\net8.0-windows\\Database.mdb";
            
            try
            {
                using (OleDbConnection oledbconnection = new OleDbConnection(oldbconnection)) // connect to data base   
                {
                    await oledbconnection.OpenAsync(); 
                    if (oledbconnection.State == System.Data.ConnectionState.Open) // checks whether the connection to the database is open.
                    {
                        try
                        {
                            using (OleDbCommand command = new OleDbCommand(query, oledbconnection))
                            {
                                command.Parameters.AddWithValue("@Corei", coreid); // Prevent SQL injections using parameterized queries
                                command.Parameters.AddWithValue("@ThreadID", thid);
                                command.Parameters.AddWithValue("@CreationDate", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));  
                                command.Parameters.AddWithValue("@GenerateString", flstring);

                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        catch (OleDbException Oledbex)
                        {
                            // MessageBox.Show($"Error occurred while adding data to SQL database:{sqlex.Message}");
                            Log.Error(Oledbex, "Error occurred while adding data to SQL database");
                            Log.CloseAndFlush();
                        }
                        

                    }

                }

            }
            catch (OleDbException Oledbex)
            {
                // MessageBox.Show($"Connection to Data Base failed:{sqlex.Message}");
                Log.Error(Oledbex, "Connection to Data Base failed");
                Log.CloseAndFlush();
            }

            
        }

        // The code block is responsible for generating a string, creating threads, outputting a string, writing data to the database, stopping threads-------

    }
}