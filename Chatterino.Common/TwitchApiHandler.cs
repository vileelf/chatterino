using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chatterino.Common
{
    public class TwitchApiHandler
    {

        private static HttpStatusCode Request(string apiHandle, string query, string body, string Method)
        {
            var request = WebRequest.Create($"https://api.twitch.tv/helix/{apiHandle}?{query}");
            if (AppSettings.IgnoreSystemProxy)
            {
                request.Proxy = null;
            }
            request.Headers["Client-ID"] = $"{IrcManager.Account.ClientId}";
            request.Headers["Authorization"] = $"Bearer {IrcManager.Account.OauthToken}";
            request.Method = Method;
            if (body != null)
            {
                request.ContentType = "application/json";
                var bodyData = Encoding.UTF8.GetBytes(body);
                request.ContentLength = bodyData.Length;
                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bodyData, 0, bodyData.Length);
                }
            }
            try
            {
                using (var response = request.GetResponse())
                {
                    return ((HttpWebResponse)response).StatusCode;
                }
            }
            catch (WebException ex)
            {
                return ((HttpWebResponse)ex.Response).StatusCode;
            }
        }
        /// <summary>
        /// Performs a Post request against twitches json api. 
        /// </summary>
        /// <param name="apiHandle">The api handle you want to access</param>
        /// <param name="query">The query parameters you want to pass to the api.</param>
        /// <param name="body">The body you want to pass to the api.</param>
        /// <returns>Returns the HttpStatusCode that twitch returns.</returns>
        public static HttpStatusCode Post(string apiHandle, string query, string body)
        {
            return Request(apiHandle, query, body, "POST");
        }

        /// <summary>
        /// Performs a Patch request against twitches json api. 
        /// </summary>
        /// <param name="apiHandle">The api handle you want to access</param>
        /// <param name="query">The query parameters you want to pass to the api.</param>
        /// <param name="body">The body you want to pass to the api.</param>
        /// <returns>Returns the HttpStatusCode that twitch returns.</returns>
        public static HttpStatusCode Patch(string apiHandle, string query, string body)
        {
            return Request(apiHandle, query, body, "PATCH");
        }

        /// <summary>
        /// Performs a Delete request against twitches json api. 
        /// </summary>
        /// <param name="apiHandle">The api handle you want to access</param>
        /// <param name="query">The query parameters you want to pass to the api.</param>
        /// <returns>Returns the HttpStatusCode that twitch returns.</returns>
        public static HttpStatusCode Delete(string apiHandle, string query)
        {
            return Request(apiHandle, query, null, "DELETE");
        }

        /// <summary>
        /// Performs a put request against twitches json api. 
        /// </summary>
        /// <param name="apiHandle">The api handle you want to access</param>
        /// <param name="query">The query parameters you want to pass to the api.</param>
        /// <param name="body">The body you want to pass to the api.</param>
        /// <returns>Returns the HttpStatusCode that twitch returns.</returns>
        public static HttpStatusCode Put(string apiHandle, string query, string body)
        {
            return Request(apiHandle, query, body, "PUT");
        }

        /// <summary>
        /// Performs a get request against the twitch api. 
        /// </summary>
        /// <param name="apiHandle">The handle of the twitch api you want to access.</param>
        /// <param name="query">The query parameters you want to pass to the api.</param>
        /// <returns>Returns parsed json if successful and an HttpStatusCode if it fails. Make sure to check the type of the return.</returns>
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
            try
            {
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
            catch (WebException ex)
            {
                return ((HttpWebResponse)ex.Response).StatusCode;
            }
        }
    }
}
