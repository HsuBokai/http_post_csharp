using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.media_path.Text = this.folderBrowserDialog1.SelectedPath;
            }   
        }

        private void import_medias_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(this.media_path.Text)) return;
            DirectoryInfo di = new DirectoryInfo(this.media_path.Text);
            try
            {
                foreach (var fi in di.EnumerateFiles())
                {
                    if(fi.Extension == ".wav"){
                        
                        XmlDocument xml_doc = new XmlDocument();
                        XmlElement testdata = xml_doc.CreateElement("TestData");
                        xml_doc.AppendChild(testdata);
                        XmlElement externalMediaId = xml_doc.CreateElement("ExternalMediaId");
                        externalMediaId.InnerText = fi.Name;
                        testdata.AppendChild(externalMediaId);
                        xml_doc.Save(fi.FullName + ".xml");
                        this.listBox1.Items.Add("Generate " + fi.FullName + ".xml");
                    }
                }
            }
            catch (DirectoryNotFoundException DirNotFound) {
                this.listBox1.Items.Add(DirNotFound.Message);
            }
            catch (UnauthorizedAccessException UnAuthDir) {
                this.listBox1.Items.Add(UnAuthDir.Message);
            }
            catch (PathTooLongException LongPath) {
                this.listBox1.Items.Add(LongPath.Message);
            }
        }

        private void select_term_file_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.term_file.Text = this.openFileDialog1.FileName;
            }
        }

        private void search_btn_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(this.media_path.Text)) return;
            if (!File.Exists(this.term_file.Text)) return;
            if (!Directory.Exists(this.output_path.Text)) return;
            
            XmlDocument doc = new XmlDocument();
            XmlElement searchRequest = doc.CreateElement("SearchRequest");
            doc.AppendChild(searchRequest);
            XmlElement expression = doc.CreateElement("Expression");

            XmlElement or_tag = doc.CreateElement("Or");
            String[] terms = File.ReadAllText(this.term_file.Text).Split('\n');
            foreach (var term in terms)
            {
                //Console.WriteLine("{0}", term);
                char[] separators = { ',', '"' };
                String[] components = term.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                if(components.Length >= 3){
                    XmlElement phrase = doc.CreateElement("Phrase");
                    phrase.SetAttribute("Threshold", components[1]);
                    phrase.InnerText = components[0];
                    or_tag.AppendChild(phrase);
                }
            }
            expression.AppendChild(or_tag);

            searchRequest.AppendChild(expression);
            XmlElement languageId = doc.CreateElement("LanguageId");
            languageId.InnerText = "e105eaea-9005-4ee6-9cbd-156ee2d3286e";
            searchRequest.AppendChild(languageId);
            XmlElement threshold = doc.CreateElement("Threshold");
            threshold.InnerText = "70";
            searchRequest.AppendChild(threshold);
            XmlElement maxResults = doc.CreateElement("MaxResults");
            maxResults.InnerText = "100";
            searchRequest.AppendChild(maxResults);
            XmlElement externalMediaIds = doc.CreateElement("ExternalMediaIds");

            DirectoryInfo di = new DirectoryInfo(this.media_path.Text);
            try
            {
                foreach (var fi in di.EnumerateFiles())
                {
                    if (fi.Extension == ".wav")
                    {
                        XmlElement externalMediaId = doc.CreateElement("ExternalMediaId");
                        externalMediaId.InnerText = fi.Name;
                        externalMediaIds.AppendChild(externalMediaId);
                    }
                }
            }
            catch (DirectoryNotFoundException DirNotFound)
            {
                Console.WriteLine("(0)", DirNotFound.Message);
            }
            catch (UnauthorizedAccessException UnAuthDir)
            {
                Console.WriteLine("(0)", UnAuthDir.Message);
            }
            catch (PathTooLongException LongPath)
            {
                Console.WriteLine("(0)", LongPath.Message);
            }

            searchRequest.AppendChild(externalMediaIds);

            string search_xml_file = this.output_path.Text + "/search.xml";
            string result_file = this.output_path.Text + "/result.xml";
            doc.Save(search_xml_file);

            this.listBox1.Items.Add("Search: " + search_xml_file);
            this.Enabled = false;
            //Console.WriteLine("{0}", File.ReadAllText(search_xml_file));
            string res_xml = HTTP_POST("http://localhost:8080/audiofinder/search", File.ReadAllText(search_xml_file));
            File.WriteAllText(result_file, res_xml);
            this.Enabled = true;
            MessageBox.Show("Search finished");
            this.listBox1.Items.Add("Result: " + result_file);
        }

        private void select_output_path_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog2.ShowDialog() == DialogResult.OK)
            {
                this.output_path.Text = this.folderBrowserDialog2.SelectedPath;
            }
        }


        public static string HTTP_POST(string Url, string Data)
        {
            string Out = String.Empty;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
            try
            {
                req.Method = "POST";
                req.Timeout = 100000;
                req.ContentType = "application/xml";
                req.Accept = "application/xml";

                byte[] sentData = Encoding.UTF8.GetBytes(Data);
                req.ContentLength = sentData.Length;
                using (System.IO.Stream sendStream = req.GetRequestStream())
                {
                    sendStream.Write(sentData, 0, sentData.Length);
                    sendStream.Close();
                }
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                System.IO.Stream ReceiveStream = res.GetResponseStream();
                using (System.IO.StreamReader sr = new System.IO.StreamReader(ReceiveStream, Encoding.UTF8))
                {
                    Char[] read = new Char[256];
                    int count = sr.Read(read, 0, 256);

                    while (count > 0)
                    {
                        String str = new String(read, 0, count);
                        Out += str;
                        count = sr.Read(read, 0, 256);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Out = string.Format("HTTP_ERROR :: The second HttpWebRequest object has raised an Argument Exception as 'Connection' Property is set to 'Close' :: {0}", ex.Message);
            }
            catch (WebException ex)
            {
                Out = string.Format("HTTP_ERROR :: WebException raised! :: {0}", ex.Message);
            }
            catch (Exception ex)
            {
                Out = string.Format("HTTP_ERROR :: Exception raised! :: {0}", ex.Message);
            }

            return Out;
        }

        /// <summary>
        /// Sending GET request.
        /// </summary>
        /// <param name="Url">Request Url.</param>
        /// <param name="Data">Data for request.</param>
        /// <returns>Response body.</returns>
        public static string HTTP_GET(string Url, string Data)
        {
            string Out = String.Empty;
            WebRequest req = WebRequest.Create(Url + (string.IsNullOrEmpty(Data) ? "" : "?" + Data));
            try
            {
                WebResponse resp = req.GetResponse();
                using (System.IO.Stream stream = resp.GetResponseStream())
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(stream))
                    {
                        Out = sr.ReadToEnd();
                        sr.Close();
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Out = string.Format("HTTP_ERROR :: The second HttpWebRequest object has raised an Argument Exception as 'Connection' Property is set to 'Close' :: {0}", ex.Message);
            }
            catch (WebException ex)
            {
                Out = string.Format("HTTP_ERROR :: WebException raised! :: {0}", ex.Message);
            }
            catch (Exception ex)
            {
                Out = string.Format("HTTP_ERROR :: Exception raised! :: {0}", ex.Message);
            }

            return Out;
        }

        private void clear_list_box_Click(object sender, EventArgs e)
        {
            this.listBox1.Items.Clear();
        }
        
    }
}
