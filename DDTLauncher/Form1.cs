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
using System.Web;
using unvell.D2DLib.WinForm;

namespace DDTLauncher
{
    public partial class Form1 : D2DForm
    {
        private Boolean withRuler = false;
        private Boolean playing = false;
        private CookieContainer cookieContainer = new CookieContainer();
        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;
        }

        private string getSwfUrl(string url)
        {
            string swf = "";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (String.IsNullOrWhiteSpace(response.CharacterSet))
                    readStream = new StreamReader(receiveStream);
                else
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                string data = readStream.ReadToEnd();

                int found = data.IndexOf("Loading.swf?");
                string left = data.Substring(found, data.Length - found);
                found = left.IndexOf("'");
                swf = left.Substring(0, found);

                response.Close();
                readStream.Close();
            }
            return swf;
        }

        private void loadSwf(string swfUrl, int server)
        {
            AxShockwaveFlashObjects.AxShockwaveFlash axFlash;
            axFlash = new AxShockwaveFlashObjects.AxShockwaveFlash();
            axFlash.BeginInit();
            this.SuspendLayout();
            axFlash.Location = new Point(0, 0);
            axFlash.Name = "Main";
            axFlash.TabIndex = 0;
            axFlash.Size = new Size(1000, 600);
            this.ClientSize = new Size(1000, 600);
            axFlash.EndInit();
            this.ResumeLayout(false);

            this.Controls.Add(axFlash);
            axFlash.WMode = "Direct";
            axFlash.ScaleMode = 0;
            axFlash.Quality = 0;
            axFlash.LoadMovie(0, "http://s" + server + "-ddt.7tgames.com//" + this.getSwfUrl(swfUrl));
            axFlash.DisableLocalSecurity();
        }

        private string getServerSession(string cookie, int server)
        {
            string session = "";
            var url = "http://ddten.7tgames.com/playgame/s" + server;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = cookieContainer;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (String.IsNullOrWhiteSpace(response.CharacterSet))
                    readStream = new StreamReader(receiveStream);
                else
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                string data = readStream.ReadToEnd();

                var iframeStart = "<iframe id=\"iframecontent\" name=\"iframe_game_panel\" scrolling=\"no\" frameborder=\"0\" src=\"";
                int iframeIndex = data.IndexOf(iframeStart) + iframeStart.Length;
                string left = data.Substring(iframeIndex, data.Length - iframeIndex);
                iframeIndex = left.IndexOf("\"");
                session = left.Substring(0, iframeIndex);

                response.Close();
                readStream.Close();
            }
            return session;

        }
        private Boolean login(string username, string password, int server)
        {
            var success = false;
            var loginFormUrl = "http://7tgames.com/SignIn?ReturnUrl=http%3A%2F%2Fddten.7tgames.com/playgame/s2";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(loginFormUrl);

            var postData = HttpUtility.UrlEncode("UsernameOrEmail") + "="
              + HttpUtility.UrlEncode(username) + "&"
              + HttpUtility.UrlEncode("Password") + "="
              + HttpUtility.UrlEncode(password);

            request.CookieContainer = cookieContainer;
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705;)";
            request.Method = "POST";
            request.KeepAlive = true;
            request.Headers.Add("Keep-Alive: 300");
            request.Referer = "http://ddten.7tgames.com";
            request.ContentType = "application/x-www-form-urlencoded";
            byte[] body = Encoding.ASCII.GetBytes(postData);
            request.ContentLength = body.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(body, 0, body.Length);
            requestStream.Close();

            request.MaximumAutomaticRedirections = 1;
            request.AllowAutoRedirect = true;


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (String.IsNullOrWhiteSpace(response.CharacterSet))
                    readStream = new StreamReader(receiveStream);
                else
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                string data = readStream.ReadToEnd();

                if (data.IndexOf("\"success\"") > 0)
                {

                    string session = getServerSession(response.Headers["Set-Cookie"], server);

                    this.loadSwf(session, server);

                    success = true;
                }
                response.Close();
                readStream.Close();
            }
            return success;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            panel1.Enabled = false;
            if (!this.login(textBox1.Text, textBox2.Text, comboBox1.SelectedIndex + 1))
            {
                panel1.Show();
                panel1.Enabled = true;
                MessageBox.Show("Unable to login, please check your credentials!");
            }
            else
            {
                playing = true;
                panel1.Hide();
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.R))
            {
                withRuler = !withRuler;
                if (playing)
                {
                    pictureBox1.Height = 28;
                    pictureBox1.Visible = withRuler;
                    this.ClientSize = withRuler ? new Size(1000, 628) : new Size(1000, 600);
                    this.BackColor = Color.Black;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Do you want to close this application?", "Exit", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
