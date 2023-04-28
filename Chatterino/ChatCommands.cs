using Chatterino.Common;
using Chatterino.Controls;
using System;
using System.Drawing;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using TwitchIrc;

namespace Chatterino
{
    public static class ChatCommands
    {
        public static void addChatCommands ()
        {
            Commands.ChatCommands.TryAdd("user", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();
                    if (S.Length > 0 && S[0].Length > 0)
                    {
                        Common.UserInfoData data = new Common.UserInfoData();
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
            
            // Chat Commands
            Commands.ChatCommands.TryAdd("w", (s, channel, execute) =>
            {
                if (execute)
                {
                    var S = s.SplitWords();

                    if (S.Length > 1)
                    {
                        var name = S[0];
                        var whisperMessage = s.SubstringFromWordIndex(1);

                        IrcMessage message;
                        IrcMessage.TryParse($":{name}!{name}@{name}.tmi.twitch.tv PRIVMSG #whispers :" + whisperMessage, out message);
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
                        using (var resp = request.GetResponse())
                        using (var stream = resp.GetResponseStream())
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