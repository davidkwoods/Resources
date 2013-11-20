using System;
using System.Collections.Generic;
using System.Linq;
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
using System.ComponentModel;
using System.Xml.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PbpSimulator
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class Window1 : Window
    {
        BackgroundWorker worker;        //Multi-threaded work happens here.
        List<TimedXML> timedEntries;    //An in-order list of all the nodes to add and the time to add them.
        Queue<TimedXML> entryQueue;     //Same list, as a queue so I can pull them out one by one.
        string outputFilename = "";
        string inputFilename = "";
        string inputSafeFilename = "";
        XDocument delayedFile;          //file being written to
        XElement inPoint;               //Xelement where the nodes are being added.
        bool rootNodeError = true;
        bool inPointError = true;
        bool ready = false;

        public Window1()
        {
            InitializeComponent();
        }

        //Initialization.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += ThreadedLoop;
            worker.ProgressChanged += UpdateProgress;
            worker.RunWorkerCompleted += ThreadedLoopFinished;

            timedEntries = new List<TimedXML>();
            entryQueue = new Queue<TimedXML>();
            delayedFile = new XDocument();
            inPoint = null;
        }

        //User clicks start, and I fire off the background worker.
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            //First check if anything in in the works.
            if (!worker.IsBusy)
            {
                if (!ready)
                {
                    ResultsBox.Text += "\nNot Ready.\n";
                    return;
                }
                outputFilename = OutputFileBox.Text;
                entryQueue.Clear();
                DateTime start = DateTime.Now;
                foreach (TimedXML tx in timedEntries)
                {
                    tx.time = start.Add(tx.delay);
                    entryQueue.Enqueue(tx);
                }
                StartButton.Content = "Stop";
                ResultsBox.Background = new SolidColorBrush(Colors.LightYellow);
                Progress.Value = 0;
                delayedFile.Save(outputFilename);   //delayedFile is the t=0 xml file, before any additions have been made.
                ResultsBox.Text += "\nJust saved the output file as " + outputFilename + "\nBeginning to insert time-delayed nodes.";
                worker.RunWorkerAsync();            //Now I start the worker, adding in the other nodes.
            }
            else
            {
                worker.CancelAsync();
                ResultsBox.Text += "\nCancelling...";
            }
        }

        //This is the asynchronous method call performed by the backgroundWorker.
        private void ThreadedLoop(object s, DoWorkEventArgs args)
        {
            int numDone = 0;
            while (entryQueue.Count > 0)
            {
                Thread.Sleep(500);                  //Waits a half a second each time through the loop.
                if (worker.CancellationPending)     //The user can cancel the procedure.
                {
                    args.Cancel = true;
                    return;
                }
                if (entryQueue.First().time.CompareTo(DateTime.Now) <= 0)   //At each loop, check to see if anything is due for insertion.
                {
                    TimedXML newEntry = entryQueue.Dequeue();               //If it is, dequeue it and go to work.
                    if (newEntry.SimData.FirstNode != null)
                    {
                        XNode tempNode = newEntry.SimData.FirstNode;
                        while (tempNode != null)                            //Add the nodes.
                        {
                            inPoint.Add(tempNode);
                            tempNode = tempNode.NextNode;
                        }
                    }

                    delayedFile.Save(outputFilename);                       //Save the file and report progress.
                    worker.ReportProgress(++numDone);
                }
            }
        }

        //Move the progress bar to show the percent complete.
        private void UpdateProgress(object s, ProgressChangedEventArgs args)
        {
            ResultsBox.Text += "\nJust saved element " + args.ProgressPercentage + " at " + DateTime.Now.ToString("hh:mm:ss");
            Progress.Value = args.ProgressPercentage * 100 / timedEntries.Count;
        }

        //When the background thread has finished its work (when there are no more nodes to be added),
        //I wrap things up here, visually showing completion.
        private void ThreadedLoopFinished(object s, RunWorkerCompletedEventArgs args)
        {
            if (args.Cancelled)
            {
                ResultsBox.Text += "\nOperation Cancelled.";
            }
            else
            {
                ResultsBox.Text += "\n Finished.\n";
            }
            ResultsBox.Background = new SolidColorBrush(Colors.White);
            StartButton.Content = "Start";
            Progress.Value = 0;
        }

        //This reads the xml file and makes a list of all nodes to be added and the delay after which to add them.
        private List<TimedXML> LoadXmlFile(string filename)
        {
            XDocument file = XDocument.Load(filename);
            var data = from el in file.Descendants("SimData")
                       orderby int.Parse(el.Attribute("Offset").Value)
                       select new TimedXML(el, int.Parse(el.Attribute("Offset").Value));
            List<TimedXML> dataList = new List<TimedXML>();
            foreach (TimedXML tx in data)
            {
                dataList.Add(tx);
            }

            //There can only be one designated root node, and there must be
            //at least one insertion point (though I only use the first).
            rootNodeError = true;
            inPointError = true;

            //Make sure there's only one root.
            if (file.Descendants("RootNode").Count() == 1)
            {
                XElement tempRoot = file.Descendants("RootNode").First();
                if ((tempRoot.FirstNode != null) && (tempRoot.FirstNode.NextNode == null))
                {
                    delayedFile = new XDocument(tempRoot.FirstNode);
                    rootNodeError = false;
                }
            }
            else
            {
                return dataList;
            }

            //find the first insertion point.
            foreach (XElement el in delayedFile.Root.DescendantsAndSelf())
            {
                if (el.Attribute("SimDataInsertionPoint") != null)
                {
                    inPoint = el;
                    inPointError = false;
                    inPoint.Attribute("SimDataInsertionPoint").Remove();
                    break;
                }
            }

            return dataList;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ResultsBox.Text = "";
        }

        //When the user clicks Load, I pop a file dialog.  On choosing a file,
        //I process it, looking for 
        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            ready = true;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "XML Files (*.xml)|*.xml|All Files|*.*";
            ofd.FileName = inputSafeFilename;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ResultsBox.Background = new SolidColorBrush(Colors.LightYellow);
                StringBuilder builder = new StringBuilder();
                builder.Append("\nLoading: ");
                builder.AppendLine(ofd.SafeFileName);
                InputFileBox.Text = ofd.FileName;
                inputFilename = ofd.FileName;
                inputSafeFilename = ofd.SafeFileName;

                //Create a simple output filename.
                if ((outputFilename == "") || (outputFilename.EndsWith("_out.xml")))
                {
                    outputFilename = ChangeStringEnding(inputFilename, ".xml", "_out.xml");
                    OutputFileBox.Text = outputFilename;
                }

                try
                {
                    //Get nodes to be inserted.
                    timedEntries = LoadXmlFile(ofd.FileName);
                    builder.Append("Load sucessful, found ");
                    builder.Append(timedEntries.Count);
                    builder.AppendLine(" SimData nodes.");
                    ResultsBox.Background = new SolidColorBrush(Colors.White);

                    //Check for errors and display them if necessary.
                    if (rootNodeError)
                    {
                        builder.AppendLine("Error: input file did not have one and only one <RootNode> element.");
                        ready = false;
                    }
                    else
                    {
                        builder.AppendLine("\nOutput File base:");
                        builder.AppendLine(delayedFile.ToString());
                    }
                    if (inPointError)
                    {
                        builder.AppendLine("Error: no element selected as Insertion Point.  Add the \"SimDataInsertionPoint\" attribute to the desired element.");
                        ready = false;
                    }
                    else
                    {
                        builder.AppendLine("\nSelected Insertion Point:");
                        builder.AppendLine(inPoint.ToString());
                    }
                }
                catch (Exception ex)
                {
                    builder.Append("Failed.  See the following exception:\n\n");
                    builder.AppendLine(ex.ToString());
                    ResultsBox.Background = new SolidColorBrush(Colors.LightPink);
                    ready = false;
                }
                ResultsBox.Text += builder.ToString();
            }
        }

        //The user can choose an output file as well, perhaps writing over an existing file.
        private void OutputButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "XML Files (*.xml)|*.xml|All Files|*.*";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFileBox.Text = sfd.FileName;
                outputFilename = sfd.FileName;
            }
        }

        //Small method to change a string's ending.
        private string ChangeStringEnding(string toBeChanged, string oldEnding, string newEnding)
        {
            string result = toBeChanged;
            if (result.EndsWith(oldEnding))
            {
                result = result.Remove(result.Length - oldEnding.Length);
                result = result + newEnding;
            }
            return result;
        }
    }

    public class TimedXML
    {
        public XElement SimData;    //Node to be added
        public TimeSpan delay;      //Delay after which to add the node
        public DateTime time;       //Actual time to add it (determined at runtime, not at object creation).

        public TimedXML(XElement el, int timeDelay)
        {
            SimData = el;
            delay = TimeSpan.FromSeconds(timeDelay);
        }
    }
}
