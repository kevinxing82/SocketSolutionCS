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
        public delegate void LogDelegate(String message);
        private LogDelegate logDelegate;
        public ServerForm()
        {
            InitializeComponent();
            InitServer();
        }

        private void InitServer()
        {
            this.logDelegate = printLog;
            SimpleEventDispatcher dispatcher = new SimpleEventDispatcher();
            dispatcher.logHandler += log;
            server = new ChatServer(new SocketConfiguration(), dispatcher);
            server.Init();
            server.logHandler += log;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipLocalEndPoint = new IPEndPoint(ipAddress, 9527);
            server.Start(ipLocalEndPoint);
        }

        private void printLog(String message)
        {
            msgListBox.Items.Add(message);
        }

        private void log(Object sender,LogEventArgs e)
        {
            this.BeginInvoke(this.logDelegate, new object[] { e.Message });
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            server.Shutdown();
        }
    }
}
