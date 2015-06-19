using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    public partial class ClientMain : Form
    {

        private TcpClient socket;

        public ClientMain()
        {
            InitializeComponent();
        }

        private void connectButton_Click(object sender, EventArgs e) {
            socket = new TcpClient("127.0.0.1", 13000);
            MessageBox.Show("SOCKET -> " + socket.ToString());
            socket.Close();

        }
    }
}
