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
using System.Windows.Forms; //I include this because it has nicer file dialogs.  Purely cosmetic.
using System.Xml;
using System.Xml.Linq;
using System.Windows.Controls.Primitives;

namespace ManifestCodec
{
    /// <summary>
    /// This is the main window, and provides functionality for translating
    /// <f> tags in SSME manifests (or any xml file, really) between base64 strings
    /// and plain unicode text.
    /// </summary>
    
    
    enum Codec { Encode, Decode, Format };
    enum Filetype { Manifest, PBP, Match, Ad };

    public partial class Window1 : Window
    {
        string inFile;
        string outFile;
        string ending;
        Codec codec;

        public Window1()
        {
            InitializeComponent();
            ending = "_decoded.xml";
        }

        //"Waiting" visual state.
        private void SetStatusWaiting()
        {
            StatusText.Text = "Standing By.";
            StatusText.Background = Brushes.LightBlue;
        }

        //"Busy" visual state.
        private void SetStatusProcessing()
        {
            StatusText.Text = "Processing...";
            StatusText.Background = Brushes.LightYellow;
        }

        //"Completed" visual state.
        private void SetStatusCompleted()
        {
            StatusText.Text = "Completed.";
            StatusText.Background = Brushes.LightGreen;
        }

        //User clicked Open.  Display a file dialog to retrieve the target filename.
        //Update the output filename to match.
        private void InputButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XML Files (*.xml)|*.xml|All Files|*.*";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InputTextbox.Text = ofd.FileName;
                if (OutputTextbox.Text.EndsWith(ending))
                {
                    OutputTextbox.Text = "";
                }
                UpdateOutputFileName();
                SetStatusWaiting();
            }
        }

        //User chose to select a specific output file.
        //Present a dialog and retrieve the filename.
        private void OutputButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "XML Files (*.xml)|*.xml|All Files|*.*";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputTextbox.Text = sfd.FileName;
                SetStatusWaiting();
            }
        }

        //User clicked Go.  Start the process they chose.
        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            SetStatusProcessing();
            GatherOptions();
            XDocument file;
            try
            {
                file = XDocument.Load(inFile);
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error Loading file.  " + ex.ToString();
                StatusText.Background = Brushes.LightPink;
                return;
            }
            try
            {
                //if you just want to clean the file up, then this is false.
                //So if the user picked anything else, process the file based on the user's choice.
                //Otherwise, skip any work being done and just save the file.
                if (codec != Codec.Format)
                {
                    ProcessManifest(file, (bool)OptionEncode.IsChecked);
                }
            }
                //Errored out trying to process the file.  Display the exception.
            catch (Exception ex)
            {
                StatusText.Text = "Error Converting File.  " + ex.ToString();
                StatusText.Background = Brushes.LightPink;
                return;
            }
            try
            {
                file.Save(outFile);
            }
                //Errored out saving the file.
            catch (Exception ex)
            {
                StatusText.Text = "Error Saving File.  " + ex.ToString();
                StatusText.Background = Brushes.LightPink;
                return;
            }
            SetStatusCompleted();
        }

        static internal XDocument ProcessManifest(XDocument file, bool toBase64)
        {
            //Every <f> tag should be of interest, so I grab them all here.
            foreach (XElement tag in file.Descendants("f"))
            {
                //if you're decoding, the string needs to be parsed into actual
                //XML before it can be added to the document.
                if (!toBase64)
                {
                    string preProcess = tag.Value;
                    tag.Value = "";
                    if (preProcess == "")
                        continue;
                    string decoded = DecodeString(preProcess);
                    try
                    {
                        tag.Add(XElement.Parse(decoded));
                    }
                        //March Madness insertions were regular text, not xml nodes like in the olympics.
                        //If the decoded text failed to parse as xml, just set it as the <f> tag's value.
                    catch
                    {
                        tag.Value = decoded;
                    }
                }
                
                //This builds a string to represent the child nodes of the <f> tag,
                //then converts that string to base64
                else
                {
                    string preProcess = tag.ToString();
                    string encoded = "";
                    IEnumerable<XElement> tags = tag.Elements();
                    if (tags.Count() != 0)
                    {
                        StringBuilder tagbuilder = new StringBuilder();
                        foreach (XElement ele in tags)
                        {
                            tagbuilder.Append(ele.ToString());
                        }
                        encoded = EncodeString(tagbuilder.ToString());
                    }
                    else
                    {
                        preProcess = tag.Value;
                        encoded = EncodeString(preProcess);
                    }
                    tag.SetValue(encoded);
                }
            }
            return file;
        }

        //Convert unicode text to base64.
        static internal string EncodeString(string decoded)
        {
            try
            {
                byte[] toEncode = Encoding.UTF8.GetBytes(decoded.ToCharArray());
                return Convert.ToBase64String(toEncode);
            }
            catch
            {
                return "Error converting to Base64";
            }
        }

        //Convert base64 text to unicode.
        static internal string DecodeString(string encoded)
        {
            try
            {
                byte[] decoded = Convert.FromBase64String(encoded);
                return Encoding.UTF8.GetString(decoded);
            }
            catch
            {
                return "Error converting from Base64";
            }
        }

        //Retrieve the options the user has set, making them
        //convenient to the rest of the methods in the app.
        private void GatherOptions()
        {
            inFile = InputTextbox.Text;
            outFile = OutputTextbox.Text;

            if (OptionDecode.IsChecked == true)
                codec = Codec.Decode;
            else if (OptionEncode.IsChecked == true)
                codec = Codec.Encode;
            else
                codec = Codec.Format;

        }

        //If the user changed the chosen process, update the output filename
        //and visual state accordingly.
        private void ChoiceChanged(object sender, RoutedEventArgs e)
        {
            UpdateOutputFileName();
            SetStatusWaiting();
        }

        //Replaces a string's ending with a new one, but only if it ends
        //with the stated oldEnding.
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

        //Sets the intended output filename ending based on the chosen process.
        private void GetNewFileNameEnding()
        {
            switch (codec)
            {
                case Codec.Decode:
                    ending = "_decoded.xml";
                    break;
                case Codec.Encode:
                    ending = "_encoded.xml";
                    break;
                case Codec.Format:
                    ending = "_cleaned.xml";
                    break;
            }
        }

        //This is called when a new input file is chosen and when the process is changed.
        //Updates the output filename based on the chosen process, but only if it looks
        //like the filename was auto-generated by the tool.  If the current ending
        //matches the previously selected process, then we'll generate a new filename
        //with the new ending.  If the process is the same (user chose a new input file)
        //then that method helps set up for this one.
        private void UpdateOutputFileName()
        {
            string oldEnding = ending;
            GatherOptions();
            GetNewFileNameEnding();
            //User chose a new input file, and the ending looked auto-generated.
            if (OutputTextbox.Text == "")
            {
                OutputTextbox.Text = ChangeStringEnding(InputTextbox.Text, ".xml", ending);
            }
            else
            {
                if (ending != oldEnding)
                {
                    OutputTextbox.Text = ChangeStringEnding(OutputTextbox.Text, oldEnding, ending);
                }
            }
        }

        //Opens the Copy/Paste window.
        private void TextConverterButton_Click(object sender, RoutedEventArgs e)
        {
            var textcon = new DirectText();
            textcon.Show();
        }
    }
}
