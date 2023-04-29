using Chatterino.Common;
using Chatterino.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
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

            Commands.ChatCommands.TryAdd("marker", (s, channel, execute) =>
            {
                if (execute)
                {
                    var status = TwitchApiHandler.Post("streams/markers", "", $"{{\"user_id\": {channel.RoomID}, \"description\", \"{s}\"}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Marker failed to create. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Description is too long (max length 140).", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You need to be logged in.", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Forbidden)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster or a channel editor to create a marker for this channel.", HSLColor.Gray, true));
                        } else if (status == HttpStatusCode.NotFound)
                        {
                            channel.AddMessage(new Common.Message($"The stream needs to be live.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("prediction", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.Split(',');
                    if (S.Length < 4)
                    {
                        channel.AddMessage(new Common.Message($"This command is comma separated. Enter <duration>,<title>,<list of outcomes> (Needs at least 2 outcomes).", HSLColor.Gray, true));
                        return null;
                    }
                    var duration = S[0];
                    var title = S[1];
                    var optionsString = s.SubstringFromIndex(',', 2);
                    var options = optionsString.Split(',');
                    var optionsJson = "\"outcomes\": [ {\"title\": \"" + string.Join("\"}, {\"title\": \"", options) + "\"}]";
                    var status = TwitchApiHandler.Post("predictions", "", $"{{\"broadcaster_id\": {channel.RoomID}, \"title\": \"{title}\", \"prediction_window\": {duration}, {optionsJson}}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Prediction failed to create. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Could be a number of issues. There may be a prediction already running or the title could be too long (45 chars max) or one of the outcomes could be too long (25) or the duration could be too long (1800).", HSLColor.Gray, true));
                        }
                        else if (status == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You need to be the broadcaster and logged in to make predictions.", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("endprediction", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length < 1)
                    {
                        channel.AddMessage(new Common.Message($"Enter the number of the outcome you want to win.", HSLColor.Gray, true));
                        return null;
                    }
                    var parsed = int.TryParse(S[0], out var winningIndex);
                    if (!parsed || (winningIndex - 1) < 0)
                    {
                        channel.AddMessage(new Common.Message($"Make sure the number you entered is greater than 0 and is only numbers.", HSLColor.Gray, true));
                        return null;
                    }
                    dynamic json = TwitchApiHandler.Get("predictions", $"broadcaster_id={channel.RoomID}");
                    if (json is HttpStatusCode)
                    {
                        if (json == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster and logged in to end predictions.", HSLColor.Gray, true));
                        }
                        return null;
                    }

                    dynamic data = json["data"];
                    if (data.Count < 1)
                    {
                        channel.AddMessage(new Common.Message($"There are currently no predictions running.", HSLColor.Gray, true));
                        return null;
                    }
                    dynamic prediction = data[0];
                    string predictionStatus = prediction["status"];
                    if (predictionStatus != "ACTIVE" && predictionStatus != "LOCKED")
                    {
                        channel.AddMessage(new Common.Message($"There are currently no predictions running.", HSLColor.Gray, true));
                        return null;
                    }
                    string id = prediction["id"];
                    dynamic outcomes = prediction["outcomes"];

                    if (winningIndex > outcomes.Count)
                    {
                        channel.AddMessage(new Common.Message($"The number you gave is greater than the number of outcomes. Enter a number from 1 to {outcomes.Count}", HSLColor.Gray, true));
                        return null;
                    }

                    string outcomeId = outcomes[winningIndex-1]["id"];


                    var status = TwitchApiHandler.Patch("predictions", "", $"{{\"broadcaster_id\": {channel.RoomID}, \"id\": \"{id}\", \"status\": \"RESOLVED\", \"winning_outcome_id\": \"{outcomeId}\"}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Prediction failed to end. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Maybe someone ended it before you?", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("lockprediction", (s, channel, execute) =>
            {
                if (execute)
                {
                    dynamic json = TwitchApiHandler.Get("predictions", $"broadcaster_id={channel.RoomID}");
                    if (json is HttpStatusCode)
                    {
                        if (json == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster and logged in to lock predictions.", HSLColor.Gray, true));
                        }
                        return null;
                    }

                    dynamic data = json["data"];
                    if (data.Count < 1)
                    {
                        channel.AddMessage(new Common.Message($"There are currently no predictions running.", HSLColor.Gray, true));
                        return null;
                    }
                    dynamic prediction = data[0];
                    string predictionStatus = prediction["status"];
                    if (predictionStatus != "ACTIVE")
                    {
                        channel.AddMessage(new Common.Message($"There are currently no predictions active.", HSLColor.Gray, true));
                        return null;
                    }
                    string id = prediction["id"];

                    var status = TwitchApiHandler.Patch("predictions", "", $"{{\"broadcaster_id\": {channel.RoomID}, \"id\": \"{id}\", \"status\": \"LOCKED\"}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Prediction failed to lock. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Maybe someone locked it before you?", HSLColor.Gray, true));
                        }
                    }
                }
                return null;
            });

            Commands.ChatCommands.TryAdd("cancelprediction", (s, channel, execute) =>
            {
                if (execute)
                {
                    dynamic json = TwitchApiHandler.Get("predictions", $"broadcaster_id={channel.RoomID}");
                    if (json is HttpStatusCode)
                    {
                        if (json == HttpStatusCode.Unauthorized)
                        {
                            channel.AddMessage(new Common.Message($"You must be the broadcaster and logged in to cancel predictions.", HSLColor.Gray, true));
                        }
                        return null;
                    }

                    dynamic data = json["data"];
                    if (data.Count < 1)
                    {
                        channel.AddMessage(new Common.Message($"There are currently no predictions running.", HSLColor.Gray, true));
                        return null;
                    }
                    dynamic prediction = data[0];
                    string predictionStatus = prediction["status"];
                    if (predictionStatus != "ACTIVE" && predictionStatus != "LOCKED")
                    {
                        channel.AddMessage(new Common.Message($"There are currently no predictions running.", HSLColor.Gray, true));
                        return null;
                    }
                    string id = prediction["id"];

                    var status = TwitchApiHandler.Patch("predictions", "", $"{{\"broadcaster_id\": {channel.RoomID}, \"id\": \"{id}\", \"status\": \"CANCELED\"}}");
                    if (status != HttpStatusCode.OK)
                    {
                        channel.AddMessage(new Common.Message($"Prediction failed to cancel. Server returned error: {status}.", HSLColor.Gray, true));
                        if (status == HttpStatusCode.BadRequest)
                        {
                            channel.AddMessage(new Common.Message($"Maybe someone canceled it before you?", HSLColor.Gray, true));
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


            Commands.ChatCommands.TryAdd("help", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length > 0)
                    {
                        var command = S[0];
                        if (Commands.ChatCommands.ContainsKey(command))
                        {
                            var helpText = new Dictionary<string, string>() {
                                { "help", "Usage \"/help [command]\" - Lists the commands available to you and provides further context on them." },
                                { "uptime", "Usage \"/uptime\" - Retrieves the amount of time the channel has been live" },
                                { "unignore", "Usage \"/unignore <username>\" = Removes user from your ignore list. Depending on the setting in <ignored users> may be local or via twitch api." },
                                { "ignore", "Usage \"/ignore <username>\" - Adds user from your ignore list. Depending on the setting in <ignored users> may be local or via twitch api." },
                                { "rejoin", "Usage \"/rejoin\" - Rejoins the current channel and tries to retrieve messages missed." },
                                { "w", "Usage \"/w <username> <message>\" - Whispers <username> with <message>. Requires a verified phone number to use." },
                                { "r", "Usage \"/r \" - Replies to the last user that whispered you while chatterino was open. Requires a verified phone number to use." },
                                { "announce", "Usage \"/announce <announcement>\" - Call attention to your message with a highlight. (Uses same color as your name)" },
                                { "announceorange", "Usage \"/announceorange <announcement>\" - Call attention to your message with an orange highlight." },
                                { "announceblue", "Usage \"/announceblue <announcement>\" - Call attention to your message with an blue highlight." },
                                { "announcegreen", "Usage \"/announcegreen <announcement>\" - Call attention to your message with an green highlight." },
                                { "announcepurple", "Usage \"/announcepurple <announcement>\" - Call attention to your message with an purple highlight." },
                                { "delete", "Usage \"/delete <message-id>\" - Deletes the message with <message-id> (If you click on the username on the message your want to delete you can use the delete button to do this)" },
                                { "clear", "Usage \"/clear\" - Clears chat history for non-Moderator viewers." },
                                { "unvip", "Usage \"/unvip <username>\" - Revokes VIP status from a user." },
                                { "vip", "Usage \"/vip <username>\" - Grants VIP status to a user. VIPs can bypass chat restrictions. Use \"/unvip\" to revoke VIP status. Use \"/vips\" to list the VIPs of this channel." },
                                { "mod", "Usage \"/mod <username>\" - Grants moderator status to a user. Use \"/unmod\" to revoke mod status. Use \"/mods\" to list the moderators of this channel." },
                                { "unmod", "Usage \"/unmod <username>\" - Revokes moderator status from a user." },
                                { "timeout", "Usage \"/timeout <username> <duration> [reason]\" - Temporarily prevents a user from chatting. Duration is in seconds. Reason (optional) is shown to user and mods. Use \"/untimeout\" to remove a timeout." },
                                { "untimeout", "Usage \"/untimeout <username>\" - Removes a timeout on a user." },
                                { "ban", "Usage \"/ban <username> [reason]\" - Permanently prevents a user from chatting. Reason is optional and will be shown to the target user and other moderators. Use \"/unban\" to remove a ban." },
                                { "unban", "Usage \"/unban <username>\" - Removes a timeout or ban on a user." },
                                { "raid", "Usage \"/raid <channel>\" - Raids another channel. Use \"/unraid\" to cancel the Raid." },
                                { "unraid", "Usage \"/unraid\" - Cancels a Raid." },
                                { "followersoff", "Usage \"/followersoff\" - Disables followers-only mode." },
                                { "followers", "Usage \"/followers [duration]\" - Turns on followers-only mode (only users who have followed for \"duration\" may chat). Duration (optional, default=0) is in seconds. Must be less than 3 months (129600s)." },
                                { "slow", "Usage \"/slow [duration]\" - Enables slow mode (limit how often users may send messages). Duration (optional, default=30) must be a positive number of seconds (max 120s). Use \"/slowoff\" to disable." },
                                { "slowoff", "Usage \"/slowoff\" - Disables slow mode." },
                                { "subscribers", "Usage \"/subscribers\" - Enables subscribers-only mode (only subscribers may chat in this channel). Use \"/subscribersoff\" to disable subscriber-only chat." },
                                { "subscribersoff", "Usage \"/subscribersoff\" - Disables subscribers-only mode." },
                                { "emoteonly", "Usage \"/emoteonly\" - Enables emote-only mode (only emoticons may be used in chat). Use \"/emoteonlyoff\" to disable." },
                                { "emoteonlyoff", "Usage \"/emoteonlyoff\" - Disables emote-only mode." },
                                { "uniquechat", "Usage \"/uniquechat\" - Users can only send unique messages in Chat. Duplicate messages will not be allowed. Use \"/uniquechatoff\" to disable." },
                                { "uniquechatoff", "Usage \"/uniquechatoff\" - Disables unique-chat mode." },
                                { "mods", "Usage \"/mods\" - Lists the moderators of this channel. You must be the broadcaster of this channel to use this. Otherwise go to regular twitch chat to use it." },
                                { "vips", "Usage \"/vips\" - Lists the VIPs of this channel. You must be the broadcaster of this channel to use this. Otherwise go to regular twitch chat to use it." },
                                { "commercial", "Usage \"/commercial [duration]\" - Runs a commercial on this channel. You must be the broadcaster to use this. Duration is in seconds and defaults to 30s if not provided (max 180s). This command has a cooldown." },
                                { "color", "Usage \"/color <color>\" - Changes your username color. Color must be in hex (#000000) or one of the following: Blue, BlueViolet, CadetBlue, Chocolate, Coral, DodgerBlue, Firebrick, GoldenRod, Green, HotPink, OrangeRed, Red, SeaGreen, SpringGreen, YellowGreen. You must have Prime or Turbo to use the hex option." },
                                { "marker", "Usage \"/marker [description]\" - Adds a stream marker (with an optional comment, max 140 characters) at the current timestamp. You can use markers in the Highlighter for easier editing. You must be either the broadcaster or a channel editor to use this command." },
                                { "user", "Usage \"/user <username>\" - Displays information about a specific user." },
                                { "prediction", "Usage \"/prediction <duration>,<title>,<outcome1>,<outcome2>,...\" - Creates a prediction. Will remain active for <duration> seconds. You must be the broadcaster to use this. Requires at least 2 outcomes." },
                                { "endprediction", "Usage \"/endprediction <winning outcome number>\" - Ends the ongoing prediction ands selects the winning outcome based on the number entered. Number must be from 1 to the number of possible outcomes." },
                                { "lockprediction", "Usage \"/lockprediction\" - Locks the ongoing prediction so users can no longer make predictions." },
                                { "cancelprediction", "Usage \"/cancelprediction\" - Cancels the ongoing prediction and refunds points back to the users." },
                                { "me", "Usage \"/me <message>\" - Express an action in the third person." }
                            };

                            channel.AddMessage(new Common.Message(helpText[command], HSLColor.Gray, true));

                        }
                        else
                        {
                            channel.AddMessage(new Common.Message($"({command} is not a recognized command) The list of commands are me, r, {String.Join(", ", Commands.ChatCommands.Keys)}.", HSLColor.Gray, true));
                        }
                        return null;
                    }

                    channel.AddMessage(new Common.Message($"(Use \"/help <command>\" for more info on a command) The list of commands are me, r, {String.Join(", ", Commands.ChatCommands.Keys)}.", HSLColor.Gray, true));

                }
                return null;
            });
        }
    }
}