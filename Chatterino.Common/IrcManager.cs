using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.UI;
using System.Xml.Linq;
using TwitchIrc;

namespace Chatterino.Common
{
    public static class IrcManager
    {
        public const int MaxMessageLength = 500;

        public static Account Account { get; set; } = Account.AnonAccount;
        public static string DefaultClientID { get; } = "eo3lkitgi8ctbrm4dlsxc8yarrohvq";
        public static string DefaultScope { get; } = "chat:read+chat:edit+user:read:subscriptions+user:manage:blocked_users+"+
            "user:read:blocked_users+user:read:follows+moderator:manage:banned_users+user:manage:whispers+whispers:read+" +
            "channel:edit:commercial+channel:manage:vips+channel:manage:moderators+moderator:manage:announcements+" +
            "moderator:manage:chat_messages+channel:manage:raids+moderator:manage:chat_settings+user:manage:chat_color+" +
            "channel:manage:broadcast+channel:manage:predictions+user:write:chat";
        public static IrcClient Client { get; set; }
        public static string LastReceivedWhisperUser { get; set; }
        public static IEnumerable<string> IgnoredUsers => AppSettings.IgnoreViaTwitch ? twitchBlockedUsers.Keys : AppSettings.IgnoredUsers.Keys;

        private static readonly ConcurrentDictionary<string, object> twitchBlockedUsers = new ConcurrentDictionary<string, object>();
        private static DateTime nextMessageSendTime = DateTime.MinValue;
        private static DateTime nextProtectMessageSendTime = DateTime.MinValue;
        private static bool loadEmotes = true;
        private static bool readconnected = false;
        private static bool writeconnected = false;

        public static event EventHandler LoggedIn;
        public static event EventHandler Disconnected;
        public static event EventHandler Connected;
        public static event EventHandler<ValueEventArgs<string>> NoticeAdded;

        public struct TwitchEmoteValue
        {
            public string Set { get; set; }
            public string ID { get; set; }
            public string OwnerID { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string ChannelName { get; set; }
        }

        public static string LoadUserIDFromTwitch(string username)
        {
            // call twitch api
            if (username != string.Empty && DefaultClientID != string.Empty)
            {
                try
                {
                    var request =
                    WebRequest.Create(
                        $"https://api.twitch.tv/helix/users?&login={username}");
                    if (AppSettings.IgnoreSystemProxy)
                    {
                        request.Proxy = null;
                    }
                    request.Headers["Authorization"]=$"Bearer {IrcManager.Account.OauthToken}";
                    request.Headers["Client-ID"]=$"{IrcManager.DefaultClientID}";

                    using (var response = request.GetResponse())
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            dynamic json = new JsonParser().Parse(stream);
                            if (json == null || json["data"] == null || json["data"][0] == null) { return null; }
                            return json["data"][0]["id"];
                        }
                    }
                }
                catch (Exception e)
                {
                    GuiEngine.Current.log("Generic Exception Handler: " + e.ToString());
                }
            }
            return null;
        }

        [Obsolete("this api handle is dead pepehands. hopefully they revive it someday but not likely use UpdateEmotes instead", true)]
        public static void LoadEmotesFromApi(){
            try
            {
                string userid = Account.UserId, oauth = Account.OauthToken;
                var request =  WebRequest.Create($"https://api.twitch.tv/helix/users/{userid}/emotes");

                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                ((HttpWebRequest)request).Accept = "application/vnd.twitchtv.v5+json";
                request.Headers["Client-ID"] = $"{DefaultClientID}";
                request.Headers["Authorization"] = $"OAuth {oauth}";
                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        dynamic json = new JsonParser().Parse(stream);

                        foreach (var set in json["emoticon_sets"])
                        {

                            foreach (var emote in set.Value)
                            {
                                string id = emote["id"];
                                string code = Emotes.GetTwitchEmoteCodeReplacement(emote["code"]);
                                Emotes.RecentlyUsedEmotes.TryRemove(code, out LazyLoadedImage image);
                                if (!Emotes.TwitchEmotes.ContainsKey(code))
                                {
                                    Emotes.TwitchEmotes[code] = new TwitchEmoteValue
                                    {
                                        ID = id,
                                        Set = set.Key,
                                        OwnerID = set.Key,
                                        ChannelName = ""
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                GuiEngine.Current.log("Generic Exception Handler: " + e.ToString());
            }
        }
        
        public static void LoadUsersEmotes()
        {
            
            //LoadEmotesFromApi();
            if (loadEmotes == false)
            {
                loadEmotes = true;
                bool isalreadyjoined = false;
                foreach (var channel in TwitchChannel.Channels)
                {
                    if (channel.Name.Equals(Account.Username))
                    {
                        isalreadyjoined = true;
                        break;
                    }
                }
                //part and join the users channel
                if (isalreadyjoined)
                {
                    Client.WriteConnection.WriteLine("PART #" + Account.Username);
                }

                Client.WriteConnection.WriteLine("JOIN #" + Account.Username);

                if (!isalreadyjoined)
                {
                    Client.WriteConnection.WriteLine("PART #" + Account.Username);
                }
            }
            Emotes.TriggerEmotesLoaded();
        }

        public static void Connect()
        {
            Disconnect();

            readconnected = false;
            writeconnected = false;

            var username = Account.Username;
            var oauth = Account.OauthToken;
            var userId = Account.UserId;
            if (Account.IsAnon)
            {
                SaveAppSettings("");
            }
            else
            {
                if (userId == null) {
                    userId = Account.UserId = LoadUserIDFromTwitch(username);
                }
                SaveAppSettings(username);
                Task.Run(() => RetrieveTwitchBlockedUsers(oauth, userId));
                Task.Run(() => LoadUsersEmotes());
            }

            LoggedIn?.Invoke(null, EventArgs.Empty);
            AppSettings.UpdateCustomHighlightRegex();
            Task.Run(() => SetupConnection(username, oauth));
        }
        public static void Reconnect()
        {
            readconnected = false;
            writeconnected = false;
            Client.Reconnect();
        }
        public static void Disconnect()
        {
            twitchBlockedUsers.Clear();

            if (Client != null)
            {
                Client.Disconnect();

                readconnected = false;
                Client.ReadConnection.Connected -= ReadConnection_Connected;
                Client.ReadConnection.Disconnected -= ReadConnection_Disconnected;
                Client.ReadConnection.MessageReceived -= ReadConnection_MessageReceived;

                writeconnected = false;
                Client.WriteConnection.Disconnected -= WriteConnection_Disconnected;
                Client.WriteConnection.Connected -= WriteConnection_Connected;
                Client.WriteConnection.MessageReceived -= WriteConnection_MessageReceived;

                Client = null;
                Disconnected?.Invoke(null, EventArgs.Empty);
            }
        }

        private static void SaveAppSettings(string username)
        {
            try
            {
                if (AppSettings.SelectedUser != username)
                {
                    AppSettings.SelectedUser = username;
                    AppSettings.Save(null);
                }
            }
            catch (Exception)
            {

            }
        }
        private static void RetrieveTwitchBlockedUsers(string oauth, string userid)
        {
            try
            {
                var limit = 100;
                var count = 0;
                string nextLink = $"https://api.twitch.tv/helix/users/blocks?broadcaster_id={userid}&first={limit}";

                var request = WebRequest.Create(nextLink);
                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                request.Headers["Client-ID"] = $"{Account.ClientId}";
                request.Headers["Authorization"] = $"Bearer {oauth}";
                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        dynamic json = new JsonParser().Parse(stream);
                        dynamic blocks = json["data"];
                        count = blocks.Count;
                        foreach (var block in blocks)
                        {
                            string name = block["user_login"];
                            twitchBlockedUsers[name] = null;
                        }

                    }
                    response.Close();
                }
            }
            catch
            {

            }
        }
        private static void SetupConnection(string username, string oauth)
        {
            readconnected = false;
            writeconnected = false;

            var readConnection = new IrcConnection();
            var writeConnection = (Account.IsAnon || AppSettings.UseSingleConnection) ? readConnection : new IrcConnection();

            Client = new IrcClient(readConnection, writeConnection);

            Client.ReadConnection.Connected += ReadConnection_Connected;
            Client.ReadConnection.Disconnected += ReadConnection_Disconnected;
            Client.ReadConnection.MessageReceived += ReadConnection_MessageReceived;

            Client.WriteConnection.Connected += WriteConnection_Connected;
            Client.WriteConnection.Disconnected += WriteConnection_Disconnected;
            Client.WriteConnection.MessageReceived += WriteConnection_MessageReceived;

            Client.Connect(username, (oauth.StartsWith("oauth:") ? oauth : "oauth:" + oauth));
        }

        private static void ReadConnection_Connected(object sender, EventArgs e)
        {
            readconnected = true;
            if (writeconnected || Account.IsAnon)
            {
                TwitchChannelJoiner.clearQueue();
                Connected?.Invoke(null, EventArgs.Empty);
            }
        }
        private static void ReadConnection_Disconnected(object sender, EventArgs e)
        {
            readconnected = false;
            Disconnected?.Invoke(null, EventArgs.Empty);
        }
        
        private static void ReadConnection_MessageReceived(object sender, MessageEventArgs e)
        {
            var msg = e.Message;

            if (msg.Command == "PRIVMSG")
            {
                TwitchChannel.GetChannel(msg.Middle.TrimStart('#')).Process(c =>
                {
                    if (msg.PrefixUser == "twitchnotify")
                    {
                        c.AddMessage(new Message(msg.Params ?? "", HSLColor.Gray, true) { HighlightType = HighlightType.Resub });
                    }
                    else if (msg.Tags.TryGetValue("pinned-chat-paid-canonical-amount", out string paidamount)) {
                        msg.Tags.TryGetValue("pinned-chat-paid-currency", out string currency);
                        msg.Tags.TryGetValue("login", out string login);
                        msg.Tags.TryGetValue("display-name", out string displayname);
                        double paid = double.Parse(paidamount);
                        string name = displayname ?? login;
                        if (!string.IsNullOrEmpty(displayname) && !string.IsNullOrEmpty(login) &&
                                !string.Equals(displayname, login, StringComparison.OrdinalIgnoreCase)) {
                                name = displayname + "(" + login + ")";
                        }
                        var sysMessage = new Message($"{name} elevated their chat message for {paid/100:F2}$ ({currency.ToLower()})", HSLColor.Gray, true)
                        {
                            HighlightType = HighlightType.Resub
                        };
                        c.AddMessage(sysMessage);
                        if (!string.IsNullOrEmpty(msg.Params))
                        {
                            var message = new Message(msg, c)
                            {
                                HighlightType = HighlightType.Resub
                            };
                            c.AddMessage(message);
                        }
                    }
                    else
                    {
                        var message = new Message(msg, c);

                        c.Users[message.Username.ToUpper()] = message.DisplayName;

                        if (!IsMessageIgnored(message, c)) {
                            if (message.HasAnyHighlightType(HighlightType.Highlighted)) {
                                var mentionMessage = new Message(msg, c, enablePingSound: false, includeChannel: true) {
                                    HighlightType = HighlightType.None
                                };

                                TwitchChannel.MentionsChannel.AddMessage(mentionMessage);
                            }

                            if (message.UserId == Account.UserId && !AppSettings.Rainbow) {
                                AppSettings.OldColor = message.UsernameColor.ToRGBHex();
                            }
                            if (message.ReplyBody != null && AppSettings.EnableReplys) {
                                var sysMessage = new Message($"⤵️ Replying to @{message.ReplyUser}: {message.ReplyBody}", HSLColor.Gray, false, FontType.SmallItalic) {
                                    HighlightType = HighlightType.None
                                };
                                c.AddMessage(sysMessage);
                            }
                            c.AddMessage(message);
                        }
                    }
                });
            }
            else if (msg.Command == "CLEARCHAT")
            {
                var channel = msg.Middle;
                var user = msg.Params;

                msg.Tags.TryGetValue("ban-reason", out string reason);
                var duration = 0;

                if (msg.Tags.TryGetValue("ban-duration", out string _duration))
                {
                    int.TryParse(_duration, out duration);
                }

                TwitchChannel.GetChannel((msg.Middle ?? "").TrimStart('#')).Process(c => c.ClearChat(user, reason, duration));
            }
            else if (msg.Command == "CLEARMSG")
            {
                var channel = msg.Middle;
                var user = msg.Params;

                msg.Tags.TryGetValue("target-msg-id", out string msgId);
                
                TwitchChannel.GetChannel((msg.Middle ?? "").TrimStart('#')).Process(c => c.ClearMsg(msgId));
            }
            else if (msg.Command == "ROOMSTATE")
            {
                TwitchChannel.GetChannel((msg.Middle ?? "").TrimStart('#')).Process(c =>
                {
                    var state = c.RoomState;

                    string value;
                    if (msg.Tags.TryGetValue("emote-only", out value))
                    {
                        if (value == "1")
                            state |= RoomState.EmoteOnly;
                        else
                            state &= ~RoomState.EmoteOnly;
                    }
                    if (msg.Tags.TryGetValue("subs-only", out value))
                    {
                        if (value == "1")
                            state |= RoomState.SubOnly;
                        else
                            state &= ~RoomState.SubOnly;
                    }
                    if (msg.Tags.TryGetValue("slow", out value))
                    {
                        if (value == "0")
                            state &= ~RoomState.SlowMode;
                        else
                        {
                            if (!int.TryParse(value, out int time))
                                time = -1;
                            c.SlowModeTime = time;
                            state |= RoomState.SlowMode;
                        }
                    }
                    if (msg.Tags.TryGetValue("r9k", out value))
                    {
                        if (value == "1")
                            state |= RoomState.R9k;
                        else
                            state &= ~RoomState.R9k;
                    }
                    if (msg.Tags.TryGetValue("followers-only", out value))
                    {
                        if (value == "-1")
                            state &= ~RoomState.FollowerOnly;
                        else
                        {
                            if (!int.TryParse(value, out int time))
                                time = 0;
                            c.FollowModeTime = time;
                            state |= RoomState.FollowerOnly;
                        }
                    }

                    c.RoomState = state;
                });
            }
            else if (msg.Command == "USERSTATE")
            {
                if (msg.Tags.TryGetValue("mod", out string value))
                {
                    TwitchChannel.GetChannel((msg.Middle ?? "").TrimStart('#')).Process(c => c.IsMod = value == "1");
                }
                if (msg.Tags.TryGetValue("badges", out value))
                {
                    if (value.Contains("vip"))
                    {
                        TwitchChannel.GetChannel((msg.Middle ?? "").TrimStart('#')).Process(c => c.IsVip = true);
                    }
                }
                UpdateEmotes(msg);
            }
            else if (msg.Command == "WHISPER")
            {
                TwitchChannel.WhisperChannel.AddMessage(new Message(msg, TwitchChannel.WhisperChannel, true, false, isReceivedWhisper: true));
                LastReceivedWhisperUser = msg.PrefixUser;

                if (AppSettings.ChatEnableInlineWhispers)
                {
                    var inlineMessage = new Message(msg, TwitchChannel.WhisperChannel, true, false, true)
                    {
                        HighlightTab = false,
                        HighlightType = HighlightType.Whisper
                    };

                    foreach (var channel in TwitchChannel.Channels)
                    {
                        channel.AddMessage(inlineMessage);
                    }
                }
            }
            else if (msg.Command == "USERNOTICE")
            {
                msg.Tags.TryGetValue("system-msg", out string sysMsg);
                msg.Tags.TryGetValue("msg-param-recipient-display-name", out string giftdisplayname);
                msg.Tags.TryGetValue("display-name", out string displayname);
                msg.Tags.TryGetValue("msg-param-recipient-user-name", out string giftlogin);
                msg.Tags.TryGetValue("login", out string login);
                msg.Tags.TryGetValue("msg-id", out string msgid);
                msg.Tags.TryGetValue("msg-param-color", out string msgcolor);
                HSLColor syscolor = HSLColor.Gray;

                TwitchChannel.GetChannel((msg.Middle ?? "").TrimStart('#')).Process(c =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(msgid) && msgid.Equals("announcement")) {
                            sysMsg = "Announcement";
                            if (!string.IsNullOrEmpty(msgcolor)){
                                if (msgcolor.Equals("BLUE")) {
                                    syscolor = HSLColor.Blue;
                                } else if (msgcolor.Equals("ORANGE")) {
                                    syscolor = HSLColor.Orange;
                                } else if (msgcolor.Equals("PURPLE")) {
                                    syscolor = HSLColor.Purple;
                                } else if (msgcolor.Equals("GREEN")) {
                                    syscolor = HSLColor.Green;
                                }
                            }
                        } else { 
                            if (!string.IsNullOrEmpty(displayname) && !string.IsNullOrEmpty(login) &&
                                !string.Equals(displayname, login, StringComparison.OrdinalIgnoreCase))
                            {
                                int index = sysMsg.IndexOf(displayname, StringComparison.OrdinalIgnoreCase);
                                if (index != -1)
                                {
                                    index += displayname.Length;
                                    sysMsg = sysMsg.Insert(index, " (" + login + ")");
                                }
                            }
                            if (!string.IsNullOrEmpty(giftdisplayname) && !string.IsNullOrEmpty(giftlogin) &&
                                !string.Equals(giftdisplayname, giftlogin, StringComparison.OrdinalIgnoreCase))
                            {
                                int index = sysMsg.IndexOf(giftdisplayname, StringComparison.OrdinalIgnoreCase);
                                if (index != -1)
                                {
                                    index += giftdisplayname.Length;
                                    sysMsg = sysMsg.Insert(index, " (" + giftlogin + ")");
                                }
                            }
                        }
                        var sysMessage = new Message(sysMsg, syscolor, true)
                        {
                            HighlightType = HighlightType.Resub
                        };
                        c.AddMessage(sysMessage);
                        if (!string.IsNullOrEmpty(msg.Params))
                        {
                            var message = new Message(msg, c)
                            {
                                HighlightType = HighlightType.Resub
                            };
                            c.AddMessage(message);
                            c.Users[message.Username.ToUpper()] = message.DisplayName;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(displayname) && !string.IsNullOrEmpty(login))
                            {
                                c.Users[login.ToUpper()] = displayname;
                            }
                            if (!string.IsNullOrEmpty(giftdisplayname) && !string.IsNullOrEmpty(giftlogin))
                            {
                                c.Users[giftlogin.ToUpper()] = giftdisplayname;
                            }
                        }
                    }
                    catch { }
                });
            }
        }

        private static void WriteConnection_Connected(object sender, EventArgs e)
        {
            if (Account.IsAnon) { return; }
            writeconnected = true;
            if (readconnected)
            {
                TwitchChannelJoiner.clearQueue();
                Connected?.Invoke(null, EventArgs.Empty);
            }
        }
        private static void WriteConnection_Disconnected(object sender, EventArgs e)
        {
            writeconnected = false;
        }
        private static void WriteConnection_MessageReceived(object sender, MessageEventArgs e)
        {
            var msg = e.Message;

            if (msg.Command == "NOTICE")
            {
                TwitchChannel.GetChannel((msg.Middle ?? "").TrimStart('#')).Process(c =>
                {

                    if (msg.Tags.TryGetValue("msg-id", out string tmp) && tmp == "timeout_success")
                        return;

                    if (AppSettings.Rainbow && tmp == "color_changed")
                        return;

                    var message = new Message(msg.Params, null, true) { HighlightTab = false };

                    c.AddMessage(message);
                });
            }
            else if (msg.Command == "USERSTATE")
            {

                if (msg.Tags.TryGetValue("mod", out string value))
                {
                    TwitchChannel.GetChannel((msg.Middle ?? "").TrimStart('#'))
                        .Process(c =>
                        {
                            c.IsMod = value == "1";
                        });
                }
                if (msg.Tags.TryGetValue("badges", out value))
                {
                    if (value.Contains("vip"))
                    {
                        TwitchChannel.GetChannel((msg.Middle ?? "").TrimStart('#'))
                            .Process(c =>
                            {
                                c.IsVip = true;
                            });
                    }
                }
                UpdateEmotes(msg);
            }
        }

        public static void SendMessage(TwitchChannel channel, string _message, bool isMod)
        {
            if (channel != null)
            {

                var message = Commands.ProcessMessage(_message, channel, true);
                if (message == null)
                    return;
                message = Commands.AddSpace(message, isMod);
                //for single connection we send the messages through the api so they will show up on the read connection. Otherwise you cant see your own messages.
                //I don't know why the irc is programmed this way.
                if (Client.SingleConnection && !Account.IsAnon) {
                    if (Client.IsAtMessageLimit(isMod)) {
                        channel.AddMessage(new Message($"Message not sent to protect you from a global ban. (try again in {Client.GetTimeUntilNextMessage(isMod).Seconds} seconds)", HSLColor.Gray, false));
                    } else {
                        TwitchApiHandler.Post("chat/messages", "", $"{{\"broadcaster_id\":\"{channel.RoomID}\",\"sender_id\":\"{Account.UserId}\",\"message\":\"{message}\"}}");
                    }
                }
                else if (!Client.Say(message, channel.Name.TrimStart('#'), isMod))
                {
                    if (nextProtectMessageSendTime < DateTime.Now)
                    {
                        channel.AddMessage(new Message($"Message not sent to protect you from a global ban. (try again in {Client.GetTimeUntilNextMessage(isMod).Seconds} seconds)", HSLColor.Gray, false));
                        nextProtectMessageSendTime = DateTime.Now.AddSeconds(1);
                    }
                }
            }
        }

        public static bool IsIgnoredUser(string username)
        {
            if (AppSettings.IgnoreViaTwitch)
                return twitchBlockedUsers.ContainsKey(username.ToLower());
            else
                return AppSettings.IgnoredUsers.ContainsKey(username.ToLower());
        }

        public static void AddIgnoredUser(string username, string userid)
        {
            TryAddIgnoredUser(username, userid, out string message);
            NoticeAdded?.Invoke(null, new ValueEventArgs<string>(message));
        }

        public static bool TryAddIgnoredUser(string username, string userid, out string message)
        {
            if (AppSettings.IgnoreViaTwitch != true)
            {
                AppSettings.IgnoredUsers[username.ToLower()] = null;
                message = $"Ignored user \"{username}\".";
                return true;
            }
            else
            {
                var _username = username.ToLower();

                var success = false;
                HttpStatusCode statusCode;
                if (userid == null)
                {
                    userid = LoadUserIDFromTwitch(_username);
                }
                try
                {
                    var request = WebRequest.Create($"https://api.twitch.tv/helix/users/blocks?target_user_id={userid}");
                    if (AppSettings.IgnoreSystemProxy)
                    {
                        request.Proxy = null;
                    }
                    request.Headers["Client-ID"] = $"{Account.ClientId}";
                    request.Headers["Authorization"] = $"Bearer {Account.OauthToken}";
                    request.Method = "PUT";
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            statusCode = response.StatusCode;
                            success = true;
                        }
                    }
                }
                catch (WebException exc)
                {
                    statusCode = ((HttpWebResponse)exc.Response).StatusCode;
                }
                catch (Exception) { statusCode = HttpStatusCode.BadRequest; }

                if (success)
                {
                    twitchBlockedUsers[_username] = null;
                    message = $"Successfully ignored user \"{username}\".";
                    return true;
                }
                else
                {
                    message = $"Error \"{(int)statusCode}\" while trying to ignore user \"{username}\".";
                    return false;
                }
            }
        }

        public static void RemoveIgnoredUser(string username, string userid)
        {
            TryRemoveIgnoredUser(username, userid, out string message);
            NoticeAdded?.Invoke(null, new ValueEventArgs<string>(message));
        }

        public static bool TryRemoveIgnoredUser(string username, string userid, out string message)
        {
            if (AppSettings.IgnoreViaTwitch != true)
            {
                AppSettings.IgnoredUsers.TryRemove(username.ToLower(), out object _);

                message = $"Unignored user \"{username}\".";
                return true;
            }
            else
            {


                object value;
                username = username.ToLower();

                var success = false;
                HttpStatusCode statusCode;
                if (userid == null)
                {
                    userid = LoadUserIDFromTwitch(username);
                }
                try
                {
                    var request = WebRequest.Create($"https://api.twitch.tv/helix/users/blocks?target_user_id={userid}");
                    request.Method = "DELETE";
                    if (AppSettings.IgnoreSystemProxy)
                    {
                        request.Proxy = null;
                    }
                    request.Headers["Client-ID"] = $"{Account.ClientId}";
                    request.Headers["Authorization"] = $"Bearer {Account.OauthToken}";
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            statusCode = response.StatusCode;
                            success = statusCode == HttpStatusCode.NoContent;
                        }
                    }
                }
                catch (WebException exc)
                {
                    statusCode = ((HttpWebResponse)exc.Response).StatusCode;
                    success = statusCode == HttpStatusCode.NoContent;
                }
                catch (Exception) { statusCode = HttpStatusCode.BadRequest; }

                if (success)
                {
                    twitchBlockedUsers.TryRemove(username.ToLower(), out value);

                    message = $"Successfully unignored user \"{username}\".";
                    return true;
                }
                else
                {
                    message = $"Error \"{(int)statusCode}\" while trying to unignore user \"{username}\".";
                    return false;
                }
            }
        }

        public static bool TryCheckIfFollowing(string username, string userid, out bool result, out string message)
        {
            try
            {
                if (userid == null)
                {
                    userid = LoadUserIDFromTwitch(username);
                }
                var request = WebRequest.Create($"https://api.twitch.tv/helix/users/follows?to_id={userid}&from_id={Account.UserId}");
                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                request.Headers["Client-ID"] = $"{Account.ClientId}";
                request.Headers["Authorization"] = $"Bearer {Account.OauthToken}";
                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        dynamic json = new JsonParser().Parse(stream);
                        string total = json["total"];
                        if (!total.Equals("0")) {
                            result = true;
                            message = null;
                            return true;
                        } else {
                            result = false;
                            message = null;
                            return true;
                        }
                        
                    }
                }
            }
            catch (Exception exc)
            {
                var webExc = exc as HttpListenerException;

                if (webExc != null)
                {
                    if (webExc.ErrorCode == 404)
                    {
                        result = false;
                        message = null;
                        return true;
                    }
                }

                result = false;
                message = exc.Message;
                return false;
            }
        }

        [Obsolete("this api handle is dead pepehands. hopefully they revive it someday but not likley", true)]
        public static bool TryFollowUser(string username, string userid, out string message)
        {
            try
            {
                if (userid == null)
                {
                    userid = LoadUserIDFromTwitch(username);
                }
                var request = WebRequest.Create($"https://api.twitch.tv/helix/users/{Account.UserId}/follows/channels/{userid}");
                request.Method = "PUT";
                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                ((HttpWebRequest)request).Accept = "application/vnd.twitchtv.v5+json";
                request.Headers["Client-ID"] = $"{Account.ClientId}";
                request.Headers["Authorization"] = $"OAuth {Account.OauthToken}";

                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        message = null;
                        return true;
                    }
                }
            }
            catch (Exception exc)
            {
                message = exc.Message;
                return false;
            }
        }

        [Obsolete("this api handle is dead pepehands. hopefully they revive it someday but not likley", true)]
        public static bool TryUnfollowUser(string username, string userid, out string message)
        {
            try
            {
                if (userid == null)
                {
                    userid = LoadUserIDFromTwitch(username);
                }
                var request = WebRequest.Create($"https://api.twitch.tv/helix/users/{Account.UserId}/follows/channels/{userid}");
                request.Method = "DELETE";
                if (AppSettings.IgnoreSystemProxy)
                {
                    request.Proxy = null;
                }
                ((HttpWebRequest)request).Accept = "application/vnd.twitchtv.v5+json";
                request.Headers["Client-ID"] = $"{Account.ClientId}";
                request.Headers["Authorization"] = $"OAuth {Account.OauthToken}";
                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        message = null;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static bool IsMessageIgnored(Message msg, TwitchChannel c)
        {
            // check if message has an ignored keyword
            if (AppSettings.IgnoredKeywordsRegex != null && AppSettings.IgnoredKeywordsRegex.IsMatch(msg.Params))
            {
                return true;
            }

            // check if message user is on the ignore list
            if (IsIgnoredUser(msg.Username))
            {
                //check if message is somewhere that the user has mod or broadcaster
                switch (AppSettings.ChatShowIgnoredUsersMessages)
                {
                    case 1:
                        if (!c.IsModOrBroadcaster)
                            return true;
                        break;
                    case 2:
                        if (!c.IsBroadcaster)
                            return true;
                        break;
                    default:
                        return true;
                }
            }
            return false;
        }
        private static void UpdateEmotes(IrcMessage msg)
        {
            if (loadEmotes && msg.Tags.TryGetValue("emote-sets", out string value))
            {
                //GuiEngine.Current.log("sets: " + value);
                string[] emote_sets = value.Split(',');
                string temp = "";
                int count = 1;
                string emote_set;
                string emote_set_id = "";
                //split into groups of 10 since thats the limit you can request at once from the api https://api.twitch.tv/helix/chat/emotes/set?emote_set_id=
                for (int i = 0; i < emote_sets.Length; i++)
                {
                    emote_set = emote_sets[i];
                    if (count != 1)
                    {
                        temp += "&emote_set_id=";
                    }
                    temp += emote_set;
                    if (count == 10 || i == (emote_sets.Length - 1))
                    {
                        //api request
                        try
                        {
                            var request = WebRequest.Create($"https://api.twitch.tv/helix/chat/emotes/set?emote_set_id={temp}");
                            request.Method = "GET";
                            //GuiEngine.Current.log("url : " + $"https://api.twitch.tv/helix/chat/emotes/set?emote_set_id={temp}");
                            if (AppSettings.IgnoreSystemProxy)
                            {
                                request.Proxy = null;
                            }
                            ((HttpWebRequest)request).Accept = "application/json";
                            request.Headers["Client-ID"] = $"{Account.ClientId}";
                            request.Headers["Authorization"] = $"Bearer {Account.OauthToken}";
                            //GuiEngine.Current.log("body: Client-ID: " +  Account.ClientId + " Authorization: Bearer " + Account.OauthToken);
                            using (var response = request.GetResponse())
                            {
                                using (var stream = response.GetResponseStream())
                                {
                                    dynamic json = new JsonParser().Parse(stream);
                                    if (json != null)
                                    {
                                        dynamic data = json["data"];
                                        foreach (var emote in data)
                                        {
                                            string id = emote["id"];
                                            string owner_id = emote["owner_id"];
                                            if (owner_id == "twitch")
                                            {
                                                owner_id = "0";
                                            }
                                            string emote_type = emote["emote_type"];
                                            string name = emote["name"];
                                            emote_set_id = emote["emote_set_id"];
                                            
                                            string code = Emotes.GetTwitchEmoteCodeReplacement(name);
                                            Emotes.RecentlyUsedEmotes.TryRemove(code, out LazyLoadedImage image);
                                            Emotes.TwitchEmotes[code] = new TwitchEmoteValue
                                            {
                                                OwnerID = owner_id,
                                                Type = emote_type,
                                                Name = name,
                                                ID = id,
                                                Set = emote_set_id,
                                                ChannelName = ""
                                            };
                                        }

                                    }
                                }
                                response.Close();
                            }
                        }
                        catch (Exception exc)
                        {
                            GuiEngine.Current.log("Generic Exception Handler: " + exc.ToString() + " " + emote_set_id);
                        }
                        temp = "";
                        count = 1;
                    }
                    else
                    {
                        count++;
                    }
                }
            }
            loadEmotes = false;
            Emotes.TriggerEmotesLoaded();
        }

    }
}