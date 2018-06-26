using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace org.kevinxing.socket.test
{
    [TestClass]
    public class SocketClientUnitTest
    {
        private SocketClient client = new SocketClient();
        [TestMethod]
        public void ConnectServerWithValidValue()
        {
            Assert.AreEqual(SocketConnectStatus.SUCCESS, client.connect("127.0.0.1",13000));                                                              
        }

        //[TestMethod]
        public void ConnectServerWithInvalidIP()
        {
            Assert.AreEqual(SocketConnectStatus.IP_IS_INVALID, client.connect("1", 13000));
        }

        [TestMethod]
        public void CheckValidIPString()
        {
            Assert.IsTrue(SocketClient.IsValidIP("127.0.0.1"));
        }

        [TestMethod]
        public void IPStringOutOfRange()
        {
            Assert.IsFalse(SocketClient.IsValidIP("111.111.111.111.111"));
        }

        [TestMethod]
        public void IPStringNotEnoughLength()
        {
            Assert.IsFalse(SocketClient.IsValidIP("1"));
        }

        [TestMethod]
        public void IPStringWithIncorrectFormation()
        {
            Assert.IsFalse(SocketClient.IsValidIP("12345678"));
            Assert.IsFalse(SocketClient.IsValidIP("A.B.C.D"));
        }
    }
}
