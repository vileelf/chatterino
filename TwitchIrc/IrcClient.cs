using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TwitchIrc
{
    public class IrcClient
    {
        private const int ModeratorMessageQueueLimit = 99;
        private const int UserMessageQueueLimit = 19;
        private const int MessageQueueDurationInSeconds = 32;

        private static readonly Queue<DateTime> lastMessages = new Queue<DateTime>();

        public bool SingleConnection { get; private set; }
        public IrcConnection ReadConnection { get; private set; }
        public IrcConnection WriteConnection { get; private set; }

        private static object lastMessagesLock = new object();

        public IrcClient(bool singleConnection = false)
        {
            SingleConnection = singleConnection;

            ReadConnection = new IrcConnection();

            if (singleConnection)
            {
                WriteConnection = ReadConnection;
            }
            else
            {
                WriteConnection = new IrcConnection();
            }
        }

        public void Connect(string username, string password)
        {
            ReadConnection.Connect(username, password);

            if (!SingleConnection)
            {
                WriteConnection.Connect(username, password);
            }
        }

        //please use IrcManager.Reconnect
        public void Reconnect()
        {
            ReadConnection.Reconnect();

            if (!SingleConnection)
            {
                WriteConnection.Reconnect();
            }
        }

        public bool Say(string message, string channel, bool isMod)
        {
            var messageQueueLimit = GetMessageQueueLimit(isMod);

            lock (lastMessagesLock)
            {
                while (lastMessages.Count > 0 && lastMessages.Peek() < DateTime.Now)
                {
                    lastMessages.Dequeue();
                }

                if (lastMessages.Count < messageQueueLimit)
                {
                    WriteConnection.WriteLine("PRIVMSG #" + channel + " :" + message);
                    lastMessages.Enqueue(DateTime.Now + TimeSpan.FromSeconds(MessageQueueDurationInSeconds));
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private int GetMessageQueueLimit(bool isMod)
        {
            return isMod ? ModeratorMessageQueueLimit : UserMessageQueueLimit;
        }

        public TimeSpan GetTimeUntilNextMessage(bool isMod)
        {
            lock (lastMessagesLock)
            {
                return lastMessages.Count >= GetMessageQueueLimit(isMod) ? lastMessages.Peek() - DateTime.Now : TimeSpan.Zero;
            }
        }

        public void WriteLine(string value)
        {
            WriteConnection.WriteLine(value);
        }

        public void Disconnect()
        {
            ReadConnection.Dispose();

            if (!SingleConnection)
                WriteConnection.Dispose();
        }

        public void Join(string channel)
        {
            ReadConnection.WriteLine("JOIN " + channel);

            if (!SingleConnection)
                WriteConnection.WriteLine("JOIN " + channel);
        }

        public void Part(string channel)
        {
            ReadConnection.WriteLine("PART " + channel);

            if (!SingleConnection)
                WriteConnection.WriteLine("PART " + channel);
        }
    }
}
