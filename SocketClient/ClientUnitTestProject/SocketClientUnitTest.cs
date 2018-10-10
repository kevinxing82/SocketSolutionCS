using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace org.kevinxing.socket.test
{
    [TestClass]
    public class SocketClientUnitTest
    {
        private ChatClient client = new ChatClient();
        //[TestMethod]
        public void ConnectServerWithValidValue()
        {
            //Assert.AreEqual(SocketConnectState.SUCCESS, client.connect("127.0.0.1",13000));                                                              
        }

        //[TestMethod]
        public void ConnectServerWithInvalidIP()
        {
             //Assert.AreEqual(SocketConnectState.IP_IS_INVALID, client.connect("1", 13000));
        }

       // [TestMethod]
        public void CheckValidIPString()
        {
            Assert.IsTrue(ChatClient.IsValidIP("127.0.0.1"));
        }

       // [TestMethod]
        public void IPStringOutOfRange()
        {
            Assert.IsFalse(ChatClient.IsValidIP("111.111.111.111.111"));
        }

        //[TestMethod]
        public void IPStringNotEnoughLength()
        {
            Assert.IsFalse(ChatClient.IsValidIP("1"));
        }

        //[TestMethod]
        public void IPStringWithIncorrectFormation()
        {
            Assert.IsFalse(ChatClient.IsValidIP("12345678"));
            Assert.IsFalse(ChatClient.IsValidIP("A.B.C.D"));
        }
    }
}
