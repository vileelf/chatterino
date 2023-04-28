using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chatterino.Common
{
    public class TwitchApiHandler
    {
        public static  HttpStatusCode Post(string apiHandle, string query, string body)
        {
            var request = WebRequest.Create($"https://api.twitch.tv/helix/{apiHandle}?{query}");
            if (AppSettings.IgnoreSystemProxy)
            {
                request.Proxy = null;
            }
            request.Headers["Client-ID"] = $"{IrcManager.Account.ClientId}";
            request.Headers["Authorization"] = $"Bearer {IrcManager.Account.OauthToken}";
            request.Method = "POST";
            request.ContentType = "application/json";
            var bodyData = Encoding.UTF8.GetBytes(body);
            request.ContentLength = bodyData.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(bodyData, 0, bodyData.Length);
            try
            {
                using (var response = request.GetResponse())
                {
                    return ((HttpWebResponse)response).StatusCode;
                }
            } catch (WebException ex)
            {
                return ((HttpWebResponse)ex.Response).StatusCode;
            }
            
        }

        public static dynamic Get(string apiHandle, string query)
        {
            dynamic json = null;
            string url = $"https://api.twitch.tv/helix/{apiHandle}?{query}";
            var request = WebRequest.Create(url);
            if (AppSettings.IgnoreSystemProxy)
            {
                request.Proxy = null;
            }
            request.Headers["Authorization"] = $"Bearer {IrcManager.Account.OauthToken}";
            request.Headers["Client-ID"] = $"{IrcManager.DefaultClientID}";
            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    var parser = new JsonParser();

                    json = parser.Parse(stream);
                    return json;
                }
            }
        }
    }
}
