using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Chatterino.Common
{
    public class Account
    {
        public string Username { get; set; }
        public string OauthToken { get; set; }
        public string ClientId { get; set; }
        private string userid;
        public string UserId {
            get {
                if (userid != null) {
                    return userid;
                } else {
                    loadUserIDFromTwitch(this, Username, ClientId);
                    return userid;
                }
            }
            set {
                userid = value;
            }
        }
        [JsonIgnore]
        public bool IsAnon { get; private set; }

        public Account(string username, string oauthToken, string clientId)
        {
            Username = username;
            OauthToken = oauthToken;
            ClientId = clientId;
            loadUserIDFromTwitch(this, username, clientId);
        }
        protected bool loadUserIDFromTwitch(Account account, string username, string clientId)
        {
            // call twitch api
            if (username != string.Empty && clientId != string.Empty) {
                try
                {
                    var request =
                    WebRequest.Create(
                        $"https://api.twitch.tv/helix/users?&login={username}");
                    if (AppSettings.IgnoreSystemProxy)
                    {
                        request.Proxy = null;
                    }
                    request.Headers["Authorization"]=$"Bearer {account.OauthToken}";
                    request.Headers["Client-ID"]=$"{clientId}";
                    using (var response = request.GetResponse()) {
                        using (var stream = response.GetResponseStream())
                        {
                            var parser = new JsonParser();
                            dynamic json = parser.Parse(stream);
                            
                            account.UserId = json["users"][0]["_id"];
                        }
                        response.Close();
                    }
                }
                catch
                {
                } 
            }
            return false;
        }
        
        public Account()
        {
            
        }

        public static Account AnonAccount { get; } = new Account("justinfan123", string.Empty, string.Empty) { IsAnon = true};
    }
}
