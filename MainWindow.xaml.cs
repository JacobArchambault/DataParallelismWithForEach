using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static System.Drawing.RotateFlipType;
using static System.Environment;
using static System.IO.Directory;
using static System.IO.Path;
using static System.IO.SearchOption;
using static System.Threading.Tasks.Parallel;
using static System.Threading.Tasks.Task;

namespace DataParallelismWithForEach
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // New Window level variable.
        private readonly CancellationTokenSource cancelToken = new CancellationTokenSource();

        public MainWindow() => InitializeComponent();

        private void CmdCancel_Click(object sender, RoutedEventArgs e) => cancelToken.Cancel();

        private void CmdProcess_Click(object sender, RoutedEventArgs e) =>
            // Start a new "task" to process the files. 
            Factory.StartNew(() => ProcessFiles());

        private void ProcessFiles()
        {
            // Use ParallelOptions instance to store the CancellationToken
            ParallelOptions parOpts = new ParallelOptions
            {
                CancellationToken = cancelToken.Token,
                MaxDegreeOfParallelism = ProcessorCount
            };

            // Load up all *.jpg files, and make a new folder for the modified data.
            string[] files = GetFiles(@"..\..\TestPictures", "*.jpg",
                AllDirectories);
            string newDir = @"..\..\ModifiedPictures";
            CreateDirectory(newDir);

            try
            {
                //  Process the image data in a parallel manner! 
                ForEach(files, parOpts, currentFile =>
                {
                    parOpts.CancellationToken.ThrowIfCancellationRequested();

                    string filename = GetFileName(currentFile);
                    using (Bitmap bitmap = new Bitmap(currentFile))
                    {
                        bitmap.RotateFlip(Rotate180FlipNone);
                        bitmap.Save(Combine(newDir, filename));


                        // We need to ensure that the secondary threads access controls
                        // created on primary thread in a safe manner.
                        Dispatcher.Invoke(delegate
                        {
                            Title = $"Processing {filename} on thread {Thread.CurrentThread.ManagedThreadId}";
                        });
                    }
                }
                );
                Dispatcher.Invoke(delegate { Title = "Done!"; });
            }
            catch (OperationCanceledException ex)
            {
                Dispatcher.Invoke(delegate { Title = ex.Message; });
            }
        }
    }
}