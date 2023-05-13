using Chatterino.Common;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chatterino.Controls
{
    public partial class LoginForm : Form
    {
        public Account Account { get; private set; }

        HttpListener listener = new HttpListener();

        public LoginForm()
        {
            InitializeComponent();

            //Height = 283;

            Icon = App.Icon;


            Task.Run(() =>
            {
                try
                {
                    listener.Prefixes.Add("http://localhost:5215/");
                    listener.Start();

                    while (listener.IsListening)
                    {
                        var context = listener.GetContext();

                        if (context.Request.Url.AbsolutePath == "/code")
                        {
                            string answer = $@"<html>
                                                    <head>
                                                        <title>chatterino login</title>
                                                        <style>
                                                        body {{
                                                            font-family: ""Helvetica Neue"",Helvetica,Arial,sans-serif;
                                                            font-size: 16px;
                                                            font-weight: 400;
                                                            line-height: 1.5em;
                                                            background-color: #FbFbFb;
                                                            color: #555;
                                                        }}
                                                        </style>
                                                    </head>
                                                    <body>
                                                        <h1>Redirecting</h1>
                                                        <p>If your webbrowser does not redirect you automatically, click <a id='link'>here</a>.</p>
                                                        <script type='text/javascript'>
                                                            var link = 'http://localhost:5215/token?' + location.hash.substring(1);
                                                            window.location = link;
                                                            document.getElementById('link').href = link;
                                                        </script>
                                                    </body>
                                                </html>";

                            var bytes = Encoding.UTF8.GetBytes(answer);

                            context.Response.ContentLength64 = bytes.Length;
                            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            context.Response.OutputStream.Flush();
                            context.Response.Close();
                        }
                        else if (context.Request.Url.AbsolutePath == "/token")
                        {
                            var access_token = context.Request.QueryString["access_token"];
                            var scope = context.Request.QueryString["scope"];

                            var request = WebRequest.Create("https://api.twitch.tv/helix/users");
                            if (AppSettings.IgnoreSystemProxy)
                            {
                                request.Proxy = null;
                            }
                            request.Headers["Client-ID"] = $"{IrcManager.DefaultClientID}";
                            request.Headers["Authorization"] = $"Bearer {access_token}";
                            using (var response = request.GetResponse())
                            using (var stream = response.GetResponseStream())
                            {
                                var parser = new JsonParser();
                                dynamic json = parser.Parse(stream);
                                dynamic token = json["data"];
                                string username = "";
                                if (token != null && token.Count!=0) {
                                    username = token[0]["login"];
                                }

                                Account = new Account(username, access_token, IrcManager.DefaultClientID, IrcManager.DefaultScope);
                            }

                            string answer = $@"<html>
                                                    <head>
                                                        <title>chatterino login</title>
                                                        <style>
                                                        body {{
                                                            font-family: ""Helvetica Neue"",Helvetica,Arial,sans-serif;
                                                            font-size: 16px;
                                                            font-weight: 400;
                                                            line-height: 1.5em;
                                                            background-color: #FbFbFb;
                                                            color: #555;
                                                        }}
                                                        </style>
                                                    </head>
                                                    <body>
                                                        <h1>Login Successful</h1>
                                                        <p>You can now close this page and continue using chatterino.</p>
                                                    </body>
                                                </html>";

                            var bytes = Encoding.UTF8.GetBytes(answer);

                            context.Response.ContentLength64 = bytes.Length;
                            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                            context.Response.OutputStream.Flush();
                            context.Response.Close();

                            this.Invoke(() => Close());
                        }
                    }
                }
                catch(Exception e)
                {
                    GuiEngine.Current.log(e.ToString());
                    buttonLogin.Invoke(() =>
                    {
                        buttonLogin.Enabled = false;
                        lblError.Visible = true;
                    });
                }
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            listener.Close();

            base.OnClosing(e);
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            Process.Start($"https://id.twitch.tv/oauth2/authorize?response_type=token&client_id={IrcManager.DefaultClientID}&redirect_uri=http://localhost:5215/code&force_verify=true&scope={IrcManager.DefaultScope}");
        }
    }
}
