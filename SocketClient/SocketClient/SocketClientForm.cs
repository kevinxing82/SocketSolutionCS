using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace org.kevinxing.socket
{
    public partial class SocketClientForm : Form
    {
        private ChatAsyncClient client;
        public SocketClientForm()
        {
            InitializeComponent();
            InitClient();
        }

        private void InitClient()
        {
            this.logDelegate = printLog;
            client = new ChatAsyncClient();
            client.logHandler += log;
        }

        private  void connectButton_ClickAsync(object sender, EventArgs e)
        {
            client.StartClientAsync();
        }

        private void disconnectButton_Click(object sender, EventArgs e)
        {

        }

        private void sendRandomMsgButton_Click(object sender, EventArgs e)
        {

        }

        private void massiveConnectTestButton_Click(object sender, EventArgs e)
        {

        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            client.SendAsync("This is a test<EOF>");
        }

        private void ipTextBox_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void portTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void inputTextBox_TextChanged(object sender, EventArgs e)
        {

        }
        public delegate void LogDelegate(String message);
        private void printLog(String message)
        {
            msgListBox.Items.Add(message);
        }
        public LogDelegate logDelegate;
        private void log(Object sender, LogEventArgs e)
        {
            this.BeginInvoke(this.logDelegate, new Object[] { e.Message });
        }
    }
}
