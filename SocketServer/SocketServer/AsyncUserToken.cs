using System.Net.Sockets;

namespace org.kevinxing.socket
{
    public class AsyncUserToken
    {
        private Socket socket;
        public Socket Socket
        {
            get { return this.socket; }
            set { this.socket = value; }
        }
        //自定义的一些内容  
        private string id;
        public string ID
        {
            get { return this.id; }
            set { this.id = value; }
        }

        private string name;
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
    }
}