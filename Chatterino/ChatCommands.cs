using Chatterino.Common;
using Chatterino.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Xml.Linq;
using TwitchIrc;

namespace Chatterino
{
    public static class ChatCommands
    {
        public static void addChatCommands ()
        {
            void announce(string message, string color, TwitchChannel channel)
            {
                var status = TwitchApiHandler.Post("chat/announcements", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", $"{{\"message\": \"{message}\"" + (color!=null ? $", \"color\": \"{color}\"}}" : "}"));
                if (status != HttpStatusCode.NoContent)
                {
                    channel.AddMessage(new Common.Message($"Mod failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                    if (status == HttpStatusCode.BadRequest)
                    {
                        channel.AddMessage(new Common.Message($"The message failed review.", HSLColor.Gray, true));
                    }
                    else if (status == HttpStatusCode.Unauthorized)
                    {
                        channel.AddMessage(new Common.Message($"You must be logged in to announce.", HSLColor.Gray, true));
                    }
                    else if (status == HttpStatusCode.Forbidden)
                    {
                        channel.AddMessage(new Common.Message($"You must be a mod to announce.", HSLColor.Gray, true));
                    }
                }
            }

            Commands.ChatCommands.TryAdd("user", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length > 0 && S[0].Length > 0)
                    {
                        var data = new UserInfoData();
                        data.UserName = S[0];
                        data.Channel = channel;
                        if ((data.UserId = IrcManager.LoadUserIDFromTwitch(data.UserName)) != null) {
                            var popup = new UserInfoPopup(data)
                            {
                                StartPosition = FormStartPosition.Manual,
                                Location = Cursor.Position
                            };

                            popup.Show();

                            var screen = Screen.FromPoint(Cursor.Position);

                            int x = popup.Location.X, y = popup.Location.Y;

                            if (popup.Location.X < screen.WorkingArea.X)
                            {
                                x = screen.WorkingArea.X;
                            }
                            else if (popup.Location.X + popup.Width > screen.WorkingArea.Right)
                            {
                                x = screen.WorkingArea.Right - popup.Width;
                            }

                            if (popup.Location.Y < screen.WorkingArea.Y)
                            {
                                y = screen.WorkingArea.Y;
                            }
                            else if (popup.Location.Y + popup.Height > screen.WorkingArea.Bottom)
                            {
                                y = screen.WorkingArea.Bottom - popup.Height;
                            }

                            popup.Location = new Point(x, y);
                        } else {
                            channel.AddMessage(new Common.Message($"This user could not be found (/user {data.UserName})"));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("color", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1)
                    {
                        channel.AddMessage(new Common.Message($"Enter a color.", HSLColor.Gray, true));
                        return null;
                    }
                    var color = S[0];
                    var status = TwitchApiHandler.Put("chat/color", $"user_id={IrcManager.Account.UserId}&color={HttpUtility.UrlEncode(color)}", null);
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"Color failed to change. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Color may be invalid or you need prime/turbo to use this color.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You need to be logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("commercial", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    string length;
                    if (S.Length < 1)
                    {
                        length = "30";
                    }
                    else
                    {
                        length = S[0];
                    }
                    var status = TwitchApiHandler.Post("channels/commercial", "", $"{{\"broadcaster_id\": {channel.RoomID}, \"length\": {length}}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Commercial failed to run. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Stream must be live to run commercial and length must be a valid number between 1 and 180.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster to run commercials and logged in.", HSLColor.Gray, true));
                        }
                        else if (status == (HttpStatusCode)429)
                        {
                            channel.AddMessage(new Common.Message($"You have to wait a while before you may run another commercial.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("mods", (s, channel, execute) =>
            {
                if (execute)
                {
                    var json = TwitchApiHandler.Get("moderation/moderators", $"broadcaster_id={channel.RoomID}&first=100");
                    if (json is HttpStatusCode)
                    {
                        HttpStatusCode status = json;
                        channel.AddMessage(new Common.Message($"Mods command failed. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster and logged in to get list of mods. Otherwise go to regular twitch chat and use /mods.", HSLColor.Gray, true));
                        }
                        return null;
                    }
                    dynamic data = json["data"];
                    var mods = new List<string>();
                    foreach (dynamic user in data)
                    {
                        string login = user["user_login"];
                        string display_name = user["user_name"];
                        mods.Add(login.ToLower() == display_name.ToLower() ? display_name : ($"{display_name}({login})"));
                    }
                    channel.AddMessage(new Common.Message($"The moderators of this channel are: {string.Join(", ", mods)}", HSLColor.Gray, true));  
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("vips", (s, channel, execute) =>
            {
                if (execute)
                {
                    var json = TwitchApiHandler.Get("channels/vips", $"broadcaster_id={channel.RoomID}&first=100");
                    if (json is HttpStatusCode)
                    {
                        HttpStatusCode status = json;
                        channel.AddMessage(new Common.Message($"Vips command failed. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster and logged in to get list of vips. Otherwise go to regular twitch chat and use /vips.", HSLColor.Gray, true));
                        }
                        return null;
                    }
                    dynamic data = json["data"];
                    var vips = new List<string>();
                    foreach (dynamic user in data)
                    {
                        string login = user["user_login"];
                        string display_name = user["user_name"];
                        vips.Add(login.ToLower() == display_name.ToLower() ? display_name : ($"{display_name}({login})"));
                    }
                    channel.AddMessage(new Common.Message($"The vips for this channel are: {string.Join(", ", vips)}", HSLColor.Gray, true));
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("uniquechat", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", "{\"unique_chat_mode\": true}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("uniquechatoff", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", "{\"unique_chat_mode\": false}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("emoteonly", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", "{\"emote_mode\": true}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("emoteonlyoff", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", "{\"emote_mode\": false}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("subscribers", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", "{\"subscriber_mode\": true}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("subscribersoff", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", "{\"subscriber_mode\": false}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("slow", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    string duration;
                    if (S.Length < 1)
                    {
                        duration = "30";
                    }
                    else
                    {
                        duration = S[0];
                    }
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", $"{{\"slow_mode\": true, \"slow_mode_wait_time\": {duration}}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Duration entered is invalid.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("slowoff", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", "{\"slow_mode\": false}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("followers", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    string duration;
                    if (S.Length < 1)
                    {
                        duration = "0";
                    }
                    else
                    {
                        duration = S[0];
                    }
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", $"{{\"follower_mode\": true, \"follower_mode_duration\": {duration}}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Duration entered is invalid.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("followersoff", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Patch("chat/settings", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", "{\"follower_mode\": false}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Unique chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to use this.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("raid", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1) {
                        channel.AddMessage(new Common.Message($"Enter a username to raid", HSLColor.Gray, true));
                        return null; 
                    }
                    var name = S[0];
                    var userId = IrcManager.LoadUserIDFromTwitch(name);
                    if (userId == null)
                    {
                        channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                        return null;
                    }
                    var status = TwitchApiHandler.Post("raids", $"from_broadcaster_id={channel.RoomID}&to_broadcaster_id={userId}", null);
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Raid failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster to raid.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Conflict)
                        {
                            channel.AddMessage(new Common.Message($"Channel is already raiding someone else.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster and logged in to raid.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("unraid", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Delete("raid", $"broadcaster_id={channel.RoomID}");
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"unraid failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.NotFound)
                        {
                            channel.AddMessage(new Common.Message($"There is not a raid ongoing.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster and logged in to unraid.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("ban", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1) { 
                        channel.AddMessage(new Common.Message($"Enter a username to ban", HSLColor.Gray, true));
                        return null; 
                    }
                    var name = S[0];
                    var reason = s.SubstringFromWordIndex(1);
                    var userId = IrcManager.LoadUserIDFromTwitch(name);
                    if (userId == null)
                    {
                        channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                        return null;
                    }
                    var status = TwitchApiHandler.Post("moderation/bans", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", $"{{\"data\": {{\"user_id\": \"{userId}\", \"reason\":\"{reason}\"}}}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Ban failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"The user may be unbannable or already banned.", HSLColor.Gray, true));
                        } else if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to ban someone.", HSLColor.Gray, true));
                        } else if (status == HttpStatusCode.Conflict)
                        {
                            channel.AddMessage(new Common.Message($"Someone else was already banning this user. Try again.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You are not logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("unban", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1)
                    {
                        channel.AddMessage(new Common.Message($"Enter a username to unban", HSLColor.Gray, true));
                        return null;
                    }
                    var name = S[0];
                    var userId = IrcManager.LoadUserIDFromTwitch(name);
                    if (userId == null)
                    {
                        channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                        return null;
                    }
                    var status = TwitchApiHandler.Delete("moderation/bans", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}&user_id={userId}");
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"Unban failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"The user is not banned.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to unban someone.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Conflict)
                        {
                            channel.AddMessage(new Common.Message($"Someone else was already unbanning this user. Try again.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You are not logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("timeout", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 2) {
                        channel.AddMessage(new Common.Message($"Enter a username to timeout and a duration", HSLColor.Gray, true));
                        return null; 
                    }
                    var name = S[0];
                    var duration = S[1];
                    var reason = s.SubstringFromWordIndex(2);
                    var userId = IrcManager.LoadUserIDFromTwitch(name);
                    if (userId == null)
                    {
                        channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                        return null;
                    }
                    var status = TwitchApiHandler.Post("moderation/bans", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}", $"{{\"data\": {{\"user_id\": \"{userId}\", \"duration\":\"{duration}\", \"reason\":\"{reason}\"}}}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Timeout failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"The user may be unbannable or already banned or duration may be invalid.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to timeout someone.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Conflict)
                        {
                            channel.AddMessage(new Common.Message($"Someone else was already banning this user. Try again.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You are not logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("untimeout", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1)
                    {
                        channel.AddMessage(new Common.Message($"Enter a username to untimeout", HSLColor.Gray, true));
                        return null;
                    }
                    var name = S[0];
                    var userId = IrcManager.LoadUserIDFromTwitch(name);
                    if (userId == null)
                    {
                        channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                        return null;
                    }
                    var status = TwitchApiHandler.Delete("moderation/bans", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}&user_id={userId}");
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"Untimeout failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"The user is not banned.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to unban someone.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Conflict)
                        {
                            channel.AddMessage(new Common.Message($"Someone else was already unbanning this user. Try again.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You are not logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("mod", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1) {
                        channel.AddMessage(new Common.Message($"Enter a username to mod", HSLColor.Gray, true));
                        return null; 
                    }
                    var name = S[0];
                    var userId = IrcManager.LoadUserIDFromTwitch(name);
                    if (userId == null)
                    {
                        channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                        return null;
                    }
                    var status = TwitchApiHandler.Post("moderation/moderators", $"broadcaster_id={channel.RoomID}&user_id={userId}", null);
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"Mod failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"The user may already be a mod or banned.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster to mod someone.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("unmod", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1) {
                        channel.AddMessage(new Common.Message($"Enter a username to unmod", HSLColor.Gray, true));
                        return null; 
                    }
                    var name = S[0];
                    var userId = IrcManager.LoadUserIDFromTwitch(name);
                    if (userId == null)
                    {
                        channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                        return null;
                    }
                    var status = TwitchApiHandler.Delete("moderation/moderators", $"broadcaster_id={channel.RoomID}&user_id={userId}");
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"unmod failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"The user is not modded.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster to unmod someone.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("vip", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1)
                    {
                        channel.AddMessage(new Common.Message($"Enter a username to vip", HSLColor.Gray, true));
                        return null;
                    }
                    var name = S[0];
                    var userId = IrcManager.LoadUserIDFromTwitch(name);
                    if (userId == null)
                    {
                        channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                        return null;
                    }
                    var status = TwitchApiHandler.Post("channels/vips", $"broadcaster_id={channel.RoomID}&user_id={userId}", null);
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"Vip failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"The user may be blocked.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster and logged in to vip someone.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Conflict)
                        {
                            channel.AddMessage(new Common.Message($"You are out of vip slots.", HSLColor.Gray, true));
                        }
                        else if (status == (HttpStatusCode)422)
                        {
                            channel.AddMessage(new Common.Message($"The user is already a vip or a mod.", HSLColor.Gray, true));
                        }
                        else if (status == (HttpStatusCode)425)
                        {
                            channel.AddMessage(new Common.Message($"You must complete the Build a Community requirement before you can add vips.", HSLColor.Gray, true));
                        }
                        else if (status == (HttpStatusCode)429)
                        {
                            channel.AddMessage(new Common.Message($"You can only add 10 vips per 10 seconds.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("unvip", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1)
                    {
                        channel.AddMessage(new Common.Message($"Enter a username to unvip", HSLColor.Gray, true));
                        return null;
                    }
                    var name = S[0];
                    var userId = IrcManager.LoadUserIDFromTwitch(name);
                    if (userId == null)
                    {
                        channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                        return null;
                    }
                    var status = TwitchApiHandler.Delete("channels/vips", $"broadcaster_id={channel.RoomID}&user_id={userId}");
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"Vip failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster and logged in to unvip someone.", HSLColor.Gray, true));
                        }
                        else if (status == (HttpStatusCode)422)
                        {
                            channel.AddMessage(new Common.Message($"The user is not a vip.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You do not have permission to unvip this user.", HSLColor.Gray, true));
                        }
                        else if (status == (HttpStatusCode)429)
                        {
                            channel.AddMessage(new Common.Message($"You can only remove 10 vips per 10 seconds.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("clear", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Delete("moderation/chat", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}");
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"Clear chat failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to clear chat.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You are not logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("delete", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1) {
                        channel.AddMessage(new Common.Message($"Enter a message id or click a users name on the message and click delete", HSLColor.Gray, true));
                        return null; 
                    }
                    var messageId = S[0];
                    var status = TwitchApiHandler.Delete("moderation/chat", $"broadcaster_id={channel.RoomID}&moderator_id={IrcManager.Account.UserId}&message_id={messageId}");
                    if (status != HttpStatusCode.NoContent)
                    {
                        channel.AddMessage(new Common.Message($"Delete failed to go through. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be a mod to delete messages.", HSLColor.Gray, true));
                        } else if (status == HttpStatusCode.NotFound)
                        {
                            channel.AddMessage(new Common.Message($"Message is too old to delete.", HSLColor.Gray, true));
                        } else if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Cannot delete another mod's or broadcaster's messages.", HSLColor.Gray, true));
                        } else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You are not logged in.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("announce", (s, channel, execute) =>
            {
                if (execute)
                {
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        channel.AddMessage(new Common.Message($"Enter a message to announce", HSLColor.Gray, true));
                        return null;
                    }
                    announce(s, null, channel);
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("announceblue", (s, channel, execute) =>
            {
                if (execute)
                {
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        channel.AddMessage(new Common.Message($"Enter a message to announce", HSLColor.Gray, true));
                        return null;
                    }
                    announce(s, "blue", channel);
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("announcegreen", (s, channel, execute) =>
            {
                if (execute)
                {
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        channel.AddMessage(new Common.Message($"Enter a message to announce", HSLColor.Gray, true));
                        return null;
                    }
                    announce(s, "green", channel);
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("announceorange", (s, channel, execute) =>
            {
                if (execute)
                {
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        channel.AddMessage(new Common.Message($"Enter a message to announce", HSLColor.Gray, true));
                        return null;
                    }
                    announce(s, "orange", channel);
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("announcepurple", (s, channel, execute) =>
            {
                if (execute)
                {
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        channel.AddMessage(new Common.Message($"Enter a message to announce", HSLColor.Gray, true));
                        return null;
                    }
                    announce(s, "purple", channel);
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("w", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();

                    if (S.Length > 1)
                    {
                        var name = S[0];
                        var whisperMessage = s.SubstringFromWordIndex(1);

                        IrcMessage.TryParse($":{name}!{name}@{name}.tmi.twitch.tv PRIVMSG #whispers :" + whisperMessage, out var message);
                        HttpStatusCode? status = null;
                        if (!String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(whisperMessage))
                        {
                            var toUserId = IrcManager.LoadUserIDFromTwitch(name);
                            if (toUserId == null)
                            {
                                channel.AddMessage(new Common.Message($"User {name} not found", HSLColor.Gray, true));
                                return null;
                            } else if (IrcManager.Account.UserId == null)
                            {
                                channel.AddMessage(new Common.Message($"You must be logged in to whisper", HSLColor.Gray, true));
                                return null;
                            }
                            status = TwitchApiHandler.Post("whispers", $"from_user_id={IrcManager.Account.UserId}&to_user_id={toUserId}", $"{{\"message\": \"{whisperMessage}\"}}");
                        }
                        if (status == null) { return null; }
                        if (status != HttpStatusCode.NoContent) {
                            channel.AddMessage(new Common.Message($"Whisper failed to send. Server returned error: {status}.", HSLColor.Gray, true));
                            if (status == HttpStatusCode.Unauthorized)
                            {
                                channel.AddMessage(new Common.Message("You must have a verified phone number to send whispers via chatterino", HSLColor.Gray, true));
                            } else if (status == HttpStatusCode.BadRequest)
                            {
                                channel.AddMessage(new Common.Message("The person your whispering may have whispers turned off.", HSLColor.Gray, true));
                            }
                            return null;
                        }


                        TwitchChannel.WhisperChannel.AddMessage(new Common.Message(message, TwitchChannel.WhisperChannel, isSentWhisper: true));

                        if (AppSettings.ChatEnableInlineWhispers)
                        {
                            var inlineMessage = new Common.Message(message, TwitchChannel.WhisperChannel, true, false, isSentWhisper: true)
                            {
                                HighlightTab = false,
                                HighlightType = HighlightType.Whisper
                            };

                            foreach (var c in TwitchChannel.Channels)
                            {
                                c.AddMessage(inlineMessage);
                            }
                        }
                    }
                }

                return null;
            });

            Commands.ChatCommands.TryAdd("ignore", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length > 0)
                    {
                        IrcManager.AddIgnoredUser(S[0], null);
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("rejoin", (s, channel, execute) =>
            {
                if (execute)
                {
                    Task.Run(() =>
                    {
                        channel.Rejoin();
                    });
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("unignore", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length > 0)
                    {
                        IrcManager.RemoveIgnoredUser(S[0], null);
                    }
                }
                return null;
            }); 
            
            Commands.ChatCommands.TryAdd("uptime", (s, channel, execute) =>
            {
                if (execute && channel != null)
                {
                    try
                    {
                       var request =
                            WebRequest.Create(
                                $"https://api.twitch.tv/helix/streams?user_login={channel.Name}");
                        if (AppSettings.IgnoreSystemProxy)
                        {
                            request.Proxy = null;
                        }
                        request.Headers["Authorization"]=$"Bearer {IrcManager.Account.OauthToken}";
                        request.Headers["Client-ID"]=$"{IrcManager.DefaultClientID}";
                        using (var response = request.GetResponse())
                        using (var stream = response.GetResponseStream())
                        {
                            var parser = new JsonParser();

                            dynamic json = parser.Parse(stream);
                            dynamic data = json["data"];
                            if (data != null && data.Count > 0 && data[0]["type"]!="")
                            {
                                dynamic root = data[0];

                                string createdAt = root["started_at"];

                                var streamStart = DateTime.Parse(createdAt);

                                var uptime = DateTime.Now - streamStart;

                                var text = "Stream uptime: ";

                                if (uptime.TotalDays > 1)
                                {
                                    text += (int)uptime.TotalDays + " days, " + uptime.ToString("hh\\h\\ mm\\m\\ ss\\s");
                                }
                                else
                                {
                                    text += uptime.ToString("hh\\h\\ mm\\m\\ ss\\s");
                                }

                                channel.AddMessage(new Common.Message(text));
                            }
                        }
                    }
                    catch { }
                }

                return null;
            });
        }
    }
}