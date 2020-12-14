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
using System.Drawing.Imaging;

namespace DDTLauncher
{
    public partial class Form1 : Form
    {
        private Boolean withRuler = false;
        private Boolean playing = false;
        private string session;
        private int server;
        private int quality;
        private int zoom = 100;
        private AxShockwaveFlashObjects.AxShockwaveFlash axFlash;
        private readonly CookieContainer cookieContainer = new CookieContainer();
        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
        }

        private HttpWebResponse MakeHttpRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705;)";
            request.CookieContainer = cookieContainer;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response;
        }

        private string GetSwfUrl()
        {
            string swf = "";

            HttpWebResponse response = MakeHttpRequest(session);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (String.IsNullOrWhiteSpace(response.CharacterSet))
                    readStream = new StreamReader(receiveStream);
                else
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                string data = readStream.ReadToEnd();

                string searchString = "src=\"";
                int found = data.IndexOf(searchString) + searchString.Length;
                string left = data.Substring(found, data.Length - found);
                found = left.IndexOf("\"");
                swf = left.Substring(0, found);
                response.Close();
                readStream.Close();
            }
            Application.DoEvents();
            Text = swf;
            return swf;
        }

        private void LoadSwf()
        {
            if (playing)
            {
                this.Controls.Remove(axFlash);
                axFlash.Dispose();
            }

            axFlash = new AxShockwaveFlashObjects.AxShockwaveFlash();
            axFlash.BeginInit();
            axFlash.Location = new Point(0, 0);
            axFlash.Name = "Main";
            axFlash.TabIndex = 0;
            ResizeGameWindow();
            axFlash.EndInit();

            this.Controls.Add(axFlash);
            axFlash.BGColor = "#000000";
            axFlash.WMode = "Direct";
            axFlash.Quality = quality;
            axFlash.FlashVars = "editby=Classic";
            string url = GetSwfUrl();
            axFlash.LoadMovie(0, url);
            axFlash.DisableLocalSecurity();
        }

        private Boolean SetSession()
        {
            var url = "http://ddten.gunnyxua.net:82/s" + server + "/logingame.aspx";

            HttpWebResponse response = MakeHttpRequest(url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (String.IsNullOrWhiteSpace(response.CharacterSet))
                    readStream = new StreamReader(receiveStream);
                else
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                string data = readStream.ReadToEnd();

                session = data;

                response.Close();
                readStream.Close();

                Application.DoEvents();
                return true;
            }
            else
            {
                session = "";
                return false;
            }
        }

        private Boolean SubmitForm()
        {
            var username = textBox1.Text;
            var password = textBox2.Text;
            var success = false;
            var loginFormUrl = "http://ddten.gunnyxua.net:82/login.php";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(loginFormUrl);

            var postData = HttpUtility.UrlEncode("username") + "="
              + HttpUtility.UrlEncode(username) + "&"
              + HttpUtility.UrlEncode("password") + "="
              + HttpUtility.UrlEncode(password) + "&login=";

            request.CookieContainer = cookieContainer;
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705;)";
            request.Method = "POST";
            request.KeepAlive = true;
            request.Headers.Add("Keep-Alive: 300");
            request.Referer = loginFormUrl;
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
                success = true;
                MakeHttpRequest("http://ddten.gunnyxua.net:82/s"+ server + "/play.php");
                response.Close();
            }
            return success;
        }
        private Boolean SubmitLogin()
        {
            var success = false;
            var username = textBox1.Text;
            var password = CreateMD5(textBox2.Text).ToUpper();


            var url = "http://ddten.gunnyxua.net:82/s" + server + "/checkuser.ashx?username=" + username
                + "&password=" + password;

            HttpWebResponse response = MakeHttpRequest(url); ;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (String.IsNullOrWhiteSpace(response.CharacterSet))
                    readStream = new StreamReader(receiveStream);
                else
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                string data = readStream.ReadToEnd();

                if (data == "ok")
                {
                    success = true;
                }

                response.Close();
                readStream.Close();
            }
            Application.DoEvents();
            return success;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            server = comboBox1.SelectedIndex + 1;
            quality = comboBox3.SelectedIndex == 0 ? 1 : comboBox3.SelectedIndex - 1;
            panel1.Enabled = false;
            if (SubmitForm() && SubmitLogin() && SetSession())
            {
                LoadSwf();
                playing = true;
                panel1.Hide();
            }
            else
            {
                panel1.Enabled = true;
                MessageBox.Show("Unable to login, please check your credentials!");
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (!playing)
            {
                return;
            }
            if (e.KeyData == (Keys.Control | Keys.R))
            {
                e.Handled = true;
                withRuler = !withRuler;
                pictureBox1.Visible = withRuler;
                ResizeGameWindow();
            }
            else if (e.KeyData == (Keys.Control | Keys.F5))
            {
                e.Handled = true;
                if (MessageBox.Show("Do you want reload?", "Reload Game", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    SetSession();
                    if (!string.IsNullOrEmpty(session))
                    {
                        LoadSwf();
                    }
                    else
                    {
                        MessageBox.Show("Unable to load session!");
                    }

                }
            }
            else if (e.KeyData == (Keys.Control | Keys.D0))
            {
                e.Handled = true;
                zoom = 100;
                ResizeGameWindow();
            }
            else if (e.KeyData == (Keys.Control | Keys.Oemplus))
            {
                e.Handled = true;
                zoom = Math.Min(250, zoom + 25);
                ResizeGameWindow();
            }
            else if (e.KeyData == (Keys.Control | Keys.OemMinus))
            {
                e.Handled = true;
                zoom = Math.Max(25, zoom - 25);
                ResizeGameWindow();
            }
            else if (e.KeyData == (Keys.Control | Keys.B))
            {
                e.Handled = true;
                this.FormBorderStyle = this.FormBorderStyle == FormBorderStyle.None ? FormBorderStyle.FixedSingle : FormBorderStyle.None;
                ResizeGameWindow();
            }
            else if (e.KeyData == (Keys.Control | Keys.P))
            {
                e.Handled = true;
                this.TakeScreenShot();
            }
        }

        private void ResizeGameWindow()
        {
            double size = zoom / 100.0;
            int w = Convert.ToInt32(1000 * size);
            int h = Convert.ToInt32(600 * size);
            int rulerHeight = withRuler ? Convert.ToInt32(28 * size) : 0;
            if (withRuler)
            {
                pictureBox1.Height = rulerHeight;
            }
            axFlash.Size = new Size(w, h);
            this.ClientSize = new Size(w, h + rulerHeight);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (playing && MessageBox.Show("Do you want to close this application?", "Exit", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 0)
            {
                zoom = 100;
            }
            else
            {
                zoom = (comboBox2.SelectedIndex + 1) * 25;
            }
        }

        private void TakeScreenShot()
        {
            Point p = axFlash.PointToScreen(axFlash.Location);
            Bitmap tmpImg = new Bitmap(axFlash.Width, axFlash.Height);
            using (Graphics g = Graphics.FromImage(tmpImg))
            {
                g.CopyFromScreen(p, Point.Empty, axFlash.Size);
            }

            string DirPath = "ScreenShots";
            if (!System.IO.Directory.Exists(DirPath))
            {
                System.IO.Directory.CreateDirectory(DirPath);
            }
            var filename = DirPath + "\\" + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss-fff") + ".jpg";
            tmpImg.Save(filename, ImageFormat.Jpeg);
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

    }
}
