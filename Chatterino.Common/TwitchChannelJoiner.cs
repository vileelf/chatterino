using System.Threading;
using System;
using System.IO;
using System.Collections.Generic;
using System.Timers;
using System.Net.Sockets;
using TwitchIrc;

namespace Chatterino.Common
{
    
    
    //class to control when channels are joined so they dont all try to join at the same time.
    
    //join rate limit 20 channels / 10 seconds according to twitch documentation
    //because we have 2 connections joining we have to join at half that rate
    public class TwitchChannelJoiner
    {
       const int JOIN_INTERVAL = 1000; 
       static DateTime lastJoinTime = DateTime.Now.AddSeconds(-1);
       static Mutex joinChannelMutex = new Mutex(false, "ChatterinoChannelJoiner");
       static Mutex channelQueueMutex = new Mutex(false);
       public delegate void ChannelJoinCallback(bool success, object callbackData);
       static bool channelLoop = false;
       public class queueStruct {
           public queueStruct (string channel, ChannelJoinCallback cb, object cbdata) {
               this.channel = channel;
               callback = cb;
               callbackData = cbdata;
           }
           public string channel;
           public ChannelJoinCallback callback;
           public object callbackData;
       }
       static LinkedList<queueStruct> joinChannelQueue = new LinkedList<queueStruct>();
       static AutoResetEvent queueWaitingEvent = new AutoResetEvent(false);
       
       
       //queues your channel up to join
       public static void queueJoinChannel(string channel, ChannelJoinCallback cb, object callbackData) {
           channelQueueMutex.WaitOne();
           queueStruct qs = new queueStruct(channel, cb, callbackData);
           joinChannelQueue.AddLast(qs);
           channelQueueMutex.ReleaseMutex();
           queueWaitingEvent.Set();
       }
       
       //queues your channel up to join at the front of the queue
       public static void queueJoinChannelFront(string channel, ChannelJoinCallback cb, object callbackData) {
           channelQueueMutex.WaitOne();
           queueStruct qs = new queueStruct(channel, cb, callbackData);
           joinChannelQueue.AddFirst(qs);
           channelQueueMutex.ReleaseMutex();
           queueWaitingEvent.Set();
       }
       
       //wipes the queue
       public static void clearQueue() {
           channelQueueMutex.WaitOne();
           joinChannelQueue.Clear();
           channelQueueMutex.ReleaseMutex();
       }
       
       //skips the queue and joins now
       public static bool JoinChannelSync(string channel) {
           queueStruct cqs = new queueStruct(channel, null, null);
           return JoinChannel(cqs);
       }
       
       //Starts a new thread with an infinite loop to handle the join channel queue.
       public static void runChannelLoop() {
           new Thread(() =>
           {
               queueStruct channel = null;
               int channelCount = 0;
               channelLoop = true;
               while (channelLoop) {
                   channelQueueMutex.WaitOne();
                   if (joinChannelQueue.Count == 0) {
                       channelQueueMutex.ReleaseMutex();
                       queueWaitingEvent.WaitOne();
                   } else {
                       channelQueueMutex.ReleaseMutex();
                   }
                   channelCount = joinChannelQueue.Count;
                   while(channelCount > 0 && channelLoop) {
                       channelQueueMutex.WaitOne();
                       channelCount = joinChannelQueue.Count;
                       if (channelCount > 0) {
                           channel = joinChannelQueue.First.Value;
                           joinChannelQueue.RemoveFirst();
                           channelCount = joinChannelQueue.Count;
                       }
                       channelQueueMutex.ReleaseMutex();
                       if (channel != null) {
                           JoinChannel(channel);
                       }
                       channel = null;
                   }
               }
           }).Start();
       }
       
       //stops the infinite loop running the join channel queue
       public static void stopChannelLoop() {
           channelLoop = false;
       }
       
       private static bool JoinChannel(queueStruct channelQueueStruct) {
           string channel = channelQueueStruct.channel;
           bool success = false;
           string lastJoinedDir = Path.Combine(Util.GetUserDataPath(), "Cache", "LastJoinedDate");
           if (channel != null) {
               try {
                   joinChannelMutex.WaitOne();
               } catch (AbandonedMutexException e) {
                   //no problem keep going
               }
               
               {
                    try {
                        FileStream lastJoinedfileStream = new FileStream(lastJoinedDir, FileMode.OpenOrCreate,FileAccess.ReadWrite, FileShare.ReadWrite);
                        StreamReader lastJoinedfileStreamReader = new StreamReader(lastJoinedfileStream);
                        StreamWriter lastJoinedfileStreamWriter = new StreamWriter(lastJoinedfileStream);
                        DateTime currentTime = DateTime.Now;
                        
                        string lastJoinTimeStr = lastJoinedfileStreamReader.ReadLine();
                        if (!String.IsNullOrEmpty(lastJoinTimeStr)) {
                            lastJoinTime = DateTime.Parse(lastJoinTimeStr);
                        }
                        if ((currentTime - lastJoinTime).TotalMilliseconds < JOIN_INTERVAL) {
                            Thread.Sleep(JOIN_INTERVAL);
                        }
                        try {
                            IrcManager.Client?.Join("#" + channel);
                            success = true;
                        } catch (SocketException e) {
                            GuiEngine.Current.log(e.ToString() + e.ErrorCode);
                        } catch (Exception e) {
                            GuiEngine.Current.log(e.ToString());
                        }
                        lastJoinTime = DateTime.Now;
                        lastJoinedfileStream.SetLength(0);
                        lastJoinedfileStreamWriter.WriteLine(lastJoinTime.ToString());
                        lastJoinedfileStreamWriter.Flush();
                        lastJoinedfileStreamWriter.Close();
                        lastJoinedfileStreamReader.Close();
                    } catch (Exception e) {
                        GuiEngine.Current.log(e.ToString());
                    }
               }
               joinChannelMutex.ReleaseMutex();
               if (channelQueueStruct.callback != null) {
                   channelQueueStruct.callback(success, channelQueueStruct.callbackData);
               }
           }
           return success;
       }
    }
}
