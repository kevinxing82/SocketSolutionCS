using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace org.kevinxing.socket
{
    public partial class ServerForm : Form
    {
        private ChatServer server;
        public ServerForm()
        {
            InitializeComponent();
            InitServer();
        }

        private void InitServer()
        {
            server = new ChatServer(100, 1024);
            server.Init();
            server.logHandler += log;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            IPAddress ipAddress = Dns.GetHostEntry("127.0.0.1").AddressList[0];
            IPEndPoint ipLocalEndPoint = new IPEndPoint(ipAddress, 9527);
            server.Start(ipLocalEndPoint);
        }

        private void log(Object sender,LogEventArgs e)
        {
            msgListBox.Items.Add(e.Message);
        }
    }
}
