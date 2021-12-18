using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchIrc;
using Chatterino.Common;
using Chatterino.Controls;

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
                            channel.AddMessage(new Chatterino.Common.Message($"This user could not be found (/user {data.UserName})"));
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

                        IrcMessage message;
                        IrcMessage.TryParse($":{name}!{name}@{name}.tmi.twitch.tv PRIVMSG #whispers :" + s.SubstringFromWordIndex(1), out message);

                        TwitchChannel.WhisperChannel.AddMessage(new Chatterino.Common.Message(message, TwitchChannel.WhisperChannel, isSentWhisper: true));

                        if (AppSettings.ChatEnableInlineWhispers)
                        {
                            var inlineMessage = new Chatterino.Common.Message(message, TwitchChannel.WhisperChannel, true, false, isSentWhisper: true) { HighlightTab = false };

                            inlineMessage.HighlightType = HighlightType.Whisper;

                            foreach (var c in TwitchChannel.Channels)
                            {
                                c.AddMessage(inlineMessage);
                            }
                        }
                    }
                }

                return "/w " + s;
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
                        var request = WebRequest.Create($"https://api.twitch.tv/kraken/streams/{channel.Name}?client_id={IrcManager.DefaultClientID}");
                        if (AppSettings.IgnoreSystemProxy)
                        {
                            request.Proxy = null;
                        }
                        using (var resp = request.GetResponse())
                        using (var stream = resp.GetResponseStream())
                        {
                            var parser = new JsonParser();

                            dynamic json = parser.Parse(stream);

                            dynamic root = json["stream"];

                            string createdAt = root["created_at"];

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

                            channel.AddMessage(new Chatterino.Common.Message(text));
                        }
                    }
                    catch { }
                }

                return null;
            });
        }
    }
}