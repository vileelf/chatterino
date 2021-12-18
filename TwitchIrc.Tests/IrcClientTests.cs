using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace TwitchIrc.Tests
{
    [TestClass]
    public class IrcClientTests
    {
        private const int StandardUserMessageQueueLimit = 19;
        private const int ModMessageQueueLimit = 99;
        private const int QueueIntervalInSeconds = 32;

        /// <summary>
        /// Test the constructor for the following when it is passed false:
        /// 1. It doesn't throw an exception.
        /// 2. The connection objects are not null.
        /// 3. The two connections are different.
        /// </summary>
        [TestMethod]
        public void IrcClient_Constructor_SingleConnection_False()
        {
            var client = new IrcClient(false);
            Assert.IsNotNull(client.ReadConnection);
            Assert.IsNotNull(client.WriteConnection);
            Assert.AreNotEqual(client.ReadConnection, client.WriteConnection);
        }

        /// <summary>
        /// Test the constructor for the following when it is passed true:
        /// 1. It doesn't throw an exception.
        /// 2. The connection objects are not null.
        /// 3. The two connections are the same.
        /// </summary>
        [TestMethod]
        public void IrcClient_Constructor_SingleConnection_True()
        {
            var client = new IrcClient(true);
            Assert.IsNotNull(client.ReadConnection);
            Assert.IsNotNull(client.WriteConnection);
            Assert.AreEqual(client.ReadConnection, client.WriteConnection);
        }

        [TestMethod]
        public void IrcClient_StandardUser_Say_ThrowsNoException_WhenNullMessage()
        {
            var client = new IrcClient();
            client.Say(null, "TestChannel", false);
        }

        [TestMethod]
        public void IrcClient_StandardUser_Say_ThrowsNoException_WhenNullChannel()
        {
            var client = new IrcClient();
            client.Say("Test Message", null, false);
        }

        [TestMethod]
        public void IrcClient_Mod_Say_ThrowsNoException_WhenNullMessage()
        {
            var client = new IrcClient();
            client.Say(null, "TestChannel", true);
        }

        [TestMethod]
        public void IrcClient_Mod_Say_ThrowsNoException_WhenNullChannel()
        {
            var client = new IrcClient();
            client.Say("Test Message", null, true);
        }

        [TestMethod]
        public void IrcClient_StandardUser_Say_ReturnsTrue_WhenMessageStartsWithDotColor()
        {
            var client = new IrcClient();
            var result = client.Say(".color Test Message", null, false);
            Assert.AreEqual<bool>(true, result);
        }

        [TestMethod]
        public void IrcClient_Mod_Say_ReturnsTrue_WhenMessageStartsWithDotColor()
        {
            var client = new IrcClient();
            var result = client.Say(".color Test Message", null, true);
            Assert.AreEqual<bool>(true, result);
        }

        [TestMethod]
        public void IrcClient_StandardUser_Say_ReturnsTrue_WhenMessageQueueNotFull()
        {
            var client = new IrcClient();
            var result = client.Say("Test Message", null, false);
            Assert.AreEqual<bool>(true, result);
        }

        [TestMethod]
        public void IrcClient_Mod_Say_ReturnsTrue_WhenMessageQueueNotFull()
        {
            var client = new IrcClient();
            var result = client.Say("Test Message", null, true);
            Assert.AreEqual<bool>(true, result);
        }

        [TestMethod]
        public void IrcClient_StandardUser_Say_ReturnsFalse_OnceMessageQueueFull()
        {
            var client = new IrcClient();
            Thread.Sleep(QueueIntervalInSeconds * 1000); // Stupid private static variables forced my hand. I hate this for the record, but it is required for the moment.
            for (var i = 0; i < StandardUserMessageQueueLimit + 1; i++)
            {
                var result = client.Say("Test Message", null, false);
                Assert.AreEqual<bool>(i < StandardUserMessageQueueLimit, result);
            }
            Thread.Sleep(QueueIntervalInSeconds * 1000); // Stupid private static variables forced my hand. I hate this for the record, but it is required for the moment.
        }

        [TestMethod]
        public void IrcClient_Mod_Say_ReturnsFalse_OnceMessageQueueFull()
        {
            var client = new IrcClient();
            Thread.Sleep(QueueIntervalInSeconds * 1000); // Stupid private static variables forced my hand. I hate this for the record, but it is required for the moment.
            for (var i = 0; i < ModMessageQueueLimit + 1; i++)
            {
                var result = client.Say("Test Message", null, true);
                Assert.AreEqual<bool>(i < ModMessageQueueLimit, result, $"The variable 'i' was {i}.");
            }
            Thread.Sleep(QueueIntervalInSeconds * 1000); // Stupid private static variables forced my hand. I hate this for the record, but it is required for the moment.
        }
    }
}