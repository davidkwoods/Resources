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
using System.Net;
using System.Xml.Linq;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;

namespace IDTranslator
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        //xml structure to hold the list of videos.
        XDocument idMap;
        //url to the list of videos.
        string mapURL = "http://syndication.nbcolympics.com/newsfeeds/mrss/video-full.xml";
        //Local file to save the list.
        string localFile = @".\IDMap.xml";
        //Colors for the progress bar to indicate status.
        Brush colorSuccess;
        Brush colorWaiting;
        Brush colorWorking;
        Brush colorError;

        public Window1()
        {
            InitializeComponent();
        }

        //Basic setup, establishing status colors and displayed text.
        //Also perfoms a check and prevents users from using a non-existant cached version.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            colorSuccess = DownloadBar.Foreground;
            colorWaiting = DownloadBar.Background;
            colorWorking = new SolidColorBrush(Colors.Gold);
            colorError = new SolidColorBrush(Colors.Red);
            TextBoxFileURL.Text = mapURL;
            TextBoxLocalFile.Text = localFile;
            StatusText.Text = "Download an up-to-date version, or use your cached file.";
            DownloadBar.Foreground = colorWorking;
            if (!File.Exists(localFile))
            {
                ButtonCached.IsEnabled = false;
            }
        }

        //Creates the web client and downloads the file.
        //Attaches to the progress bar and a return method.
        private void StartFileDownload()
        {
            WebClient client = new WebClient();
            client.DownloadFileCompleted += client_DownloadFileCompleted;
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
            client.DownloadFileAsync(new Uri(mapURL), localFile);
        }

        //Updates the progress bar as the download proceeds.
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadBar.Value = e.ProgressPercentage;
        }

        //After the file downloads, clean up the file and then parse it for mappings.
        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                DownloadBar.Background = colorError;
                DownloadBar.Value = 0;
                StatusText.Text += "\nError downloading file: \n" + args.Error;
                return;
            }
            StripEsi();
            StatusText.Text += "\nIDMap xml file downloaded successfully.";
            ButtonCached.IsEnabled = true;
            ParseMapFile();
            WebClient client = sender as WebClient;
            client.Dispose();
        }

        //Search the mapping for a ClipID using a given AssetID.
        //I use linq to search through all "link" nodes in the xml file,
        //and return the clipID (EBOid in the xml file) if I find a match.
        //This is not the intended search direction, as the purpose of the
        //tool is to find AssetIDs from ClipIDs, but it's included just in case.
        private void ButtonFindClipId_Click(object sender, RoutedEventArgs e)
        {
            DownloadBar.Value = 100;
            var result = from XElement el in idMap.Descendants("link")
                         where (el.Value.Contains(TextBoxAssetId.Text))
                         select el.Parent;
            if (result.Count() == 0)
            {
                SeekError("AssetID");
                return;
            }
            TextBoxClipId.Text = result.First().Attribute("EBOid").Value;
            SeekSuccess("AssetID", "ClipID", result.First().Descendants("link").First().Value);
        }

        //Search the mapping for an AssetID using a given ClipID.
        //I use linq to get the item with the right ClipID (EBOid in the xml file)
        //and return the matching AssetID.  The AssetID is only present as part of
        //the url to the page that hosts the video, so I need to strip it out first.
        private void ButtonFindAssetId_Click(object sender, RoutedEventArgs e)
        {
            DownloadBar.Value = 100;
            var result = from XElement el in idMap.Descendants("item")
                         where (el.Attribute("EBOid").Value == TextBoxClipId.Text)
                         select el.Element("link").Value;
            if (result.Count() == 0)
            {
                SeekError("ClipID");
                return;
            }
            string link = result.First();
            string guid = link.Remove(0, link.IndexOf('=') + 1);
            guid = guid.Remove(guid.IndexOf('.'));
            TextBoxAssetId.Text = guid;
            SeekSuccess("ClipID", "AssetID", link);
        }

        //Show an error state after failing to find an ID in the mapping.
        private void SeekError(string mappingFrom)
        {
            StatusText.Text = "Error: The " + mappingFrom + " you searched for is not in the mapping.";
            DownloadBar.Background = colorError;
            DownloadBar.Value = 0;
        }

        //After a successful seek, present the information and a link to
        //the video's page, and visually indicate success.
        private void SeekSuccess(string mappedFrom, string mappedTo, string link)
        {
            StatusText.Text = "Successfully mapped " + mappedFrom + " to " + mappedTo + ".\n";
            Hyperlink hyper = new Hyperlink();
            hyper.NavigateUri = new Uri(link);      //set the link
            hyper.Inlines.Add(new Run(link));       //set the text
            hyper.RequestNavigate += new RequestNavigateEventHandler(hyper_RequestNavigate);        //set the function it calls when clicked.
            StatusText.Inlines.Add(hyper);
            DownloadBar.Background = colorSuccess;
            DownloadBar.Value = 0;
        }

        //Open the default browser to send the user to the page they clicked.
        void hyper_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start((sender as Hyperlink).NavigateUri.ToString());
            e.Handled = true;
        }

        //Show a waiting state when the user changes any text.
        //This is simply to acknowledge that something has changed, and the
        //information displayed could now be stale.
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            DownloadBar.Background = colorWaiting;
        }

        //Set up visuals and start the IDMap's download.
        private void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            DownloadBar.Value = 0;
            mapURL = TextBoxFileURL.Text;
            StatusText.Text = "Downloading ID map from " + mapURL;
            StartFileDownload();
        }

        //Use a cached IDMap instead of downloading a new one.
        //Since I save the file locally, I offer the option to use a copy that
        //is already saved locally, rather than downloading a new one.
        private void ButtonCached_Click(object sender, RoutedEventArgs args)
        {
            ParseMapFile();
        }

        //Parse the local IDMap file into an XDocument.
        //On success, show accompanying visuals and enable controls.
        private void ParseMapFile()
        {
            DownloadBar.Value = 100;
            //Clear the XDocument, but only if it's been initialized already.
            if (idMap != null)
            {
                idMap.RemoveNodes();
            }
            try
            {
                idMap = XDocument.Load(localFile);
            }
            catch (Exception e)
            {
                DownloadBar.Background = colorError;
                DownloadBar.Value = 0;
                StatusText.Text += "\nError parsing file: \n" + e;
                return;
            }
            StatusText.Text += "\nLocal file parsed successfully.  Ready for lookup.";
            DownloadBar.Background = colorSuccess;
            DownloadBar.Value = 0;
            TextBoxAssetId.IsEnabled = true;
            TextBoxClipId.IsEnabled = true;
            ButtonFindAssetId.IsEnabled = true;
            ButtonFindClipId.IsEnabled = true;
        }

        //The rss file provided initially contained references to the "ESI" namespace in some
        //nodes, even though the namespace was never defined at the root.  This caused parsing
        //errors.  This method simply adds a dummy namespace to the root, allowing it to be
        //parsed without issue.
        private void StripEsi()
        {
            string tempFile = "IDMaptemp.xml";
            File.Copy(localFile, tempFile);
            StreamReader reader = new StreamReader(tempFile, Encoding.UTF8);
            StreamWriter writer = new StreamWriter(localFile, false, Encoding.UTF8);
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line.Contains("<rss"))
                {
                    writer.WriteLine(line.Replace("<rss version=\"2.0\"", "<rss version=\"2.0\" xmlns:esi=\"http://somefakeurl\""));
                    writer.Write(reader.ReadToEnd());
                    break;
                }
                writer.WriteLine(line);
            }
            writer.Flush();
            writer.Close();
            reader.Close();
            File.Delete(tempFile);
        }

        //Adds capabilities to use Return to trigger a lookup.
        //Decides which method to call based on which textbox was the sender.
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                TextBox box = sender as TextBox;
                if (box.Name == "TextBoxAssetId")
                {
                    ButtonFindClipId_Click(sender, new RoutedEventArgs());
                }
                else if (box.Name == "TextBoxClipId")
                {
                    ButtonFindAssetId_Click(sender, new RoutedEventArgs());
                }
            }
        }
    }
}
