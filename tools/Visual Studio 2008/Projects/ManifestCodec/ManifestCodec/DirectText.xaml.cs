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
using System.Windows.Shapes;
using System.Xml.Linq;

namespace ManifestCodec
{
    /// <summary>
    /// This is the copy/paste window.
    /// </summary>
    public partial class DirectText : Window
    {
        public DirectText()
        {
            InitializeComponent();
        }

        //Converts text in the <f> XML tags FROM base64 to plain text.
        private void ButtonFromXml_Click(object sender, RoutedEventArgs e)
        {
            TxtBoxPlainTxt.Text = ConvertXml(TxtBoxBase64.Text, false);
        }

        //Converts all TEXT FROM base64 to plain text.
        private void ButtonFromTxt_Click(object sender, RoutedEventArgs e)
        {
            TxtBoxPlainTxt.Text = Window1.DecodeString(TxtBoxBase64.Text);
        }

        //Converts text in the <f> XML tags from plain text TO base64.
        private void ButtonToXml_Click(object sender, RoutedEventArgs e)
        {
            TxtBoxBase64.Text = ConvertXml(TxtBoxPlainTxt.Text, true);
        }

        //Converts all TEXT TO base64.
        private void ButtonToTxt_Click(object sender, RoutedEventArgs e)
        {
            TxtBoxBase64.Text = Window1.EncodeString(TxtBoxPlainTxt.Text);
        }

        //Attempts to parse the pasted text as XML and then send it to be
        //converted to or from base64.
        private string ConvertXml(string text, bool toBase64)
        {
            string result;
            XDocument file = new XDocument();
            bool failed = true;
            try
            {
                file = XDocument.Parse(text);
                failed = false;
            }
                //If the first try failed, maybe the user pasted multiple xml nodes
                //with no root.  This adds a root and tries a second time before failing.
            catch
            {
                StringBuilder temp = new StringBuilder();
                temp.AppendLine("<xmlroot>");
                temp.AppendLine(text);
                temp.AppendLine("</xmlroot>");
                file = XDocument.Parse(temp.ToString());
                failed = false;
            }
            finally
            {
                result = "Could not parse text as xml";
            }
            if (failed)
            {
                return result;
            }

            XDocument xdoc = Window1.ProcessManifest(file, toBase64);
            result = xdoc.ToString();
            return result;
        }
    }
}
