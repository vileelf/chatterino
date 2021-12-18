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

        public bool SingleConnection { get; private set; }
        public IrcConnection ReadConnection { get; private set; }
        public IrcConnection WriteConnection { get; private set; }

        // ratelimiting
        private static readonly ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
        private static readonly Queue<DateTime> lastMessagesMod = new Queue<DateTime>();

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

            if (lastMessagesMod.Count < messageQueueLimit)
            {
                if (message.StartsWith(".color"))
                {
                    WriteConnection.WriteLine("PRIVMSG #" + channel + " :" + message);
                    return true;
                }
            }

            lock (lastMessagesLock)
            {
                while (lastMessagesMod.Count > 0 && lastMessagesMod.Peek() < DateTime.Now)
                {
                    lastMessagesMod.Dequeue();
                }

                if (lastMessagesMod.Count < messageQueueLimit)
                {
                    WriteConnection.WriteLine("PRIVMSG #" + channel + " :" + message);

                    lastMessagesMod.Enqueue(DateTime.Now + TimeSpan.FromSeconds(MessageQueueDurationInSeconds));
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
                return lastMessagesMod.Count >= GetMessageQueueLimit(isMod) ? lastMessagesMod.Peek() - DateTime.Now : TimeSpan.Zero;
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
