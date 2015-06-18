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

namespace Server {


    public partial class ServerMain : Form {

        public ServerMain() {
            InitializeComponent();
        }

        int[] fd_chatlist;
        //WTF

        public int runMain(string ip, int port) {
            Socket socket;

            sock_fd = tcp_easy_listen(ip, port, 100);
            //sadff

            return 0;
        }

        static int enable_reuse_address(Socket socket) {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        }




        //Field methods
        public int getPortInteger() {
            try {
                int port = Int32.Parse(portText.Text);
                return port;
            } catch (Exception) {
                MessageBox.Show("You must enter an integer between X and Y!", "Error!");
                return -1;
            }
        }

        public string getIpAddress() {
            if ((addressText.Text).Length < 1) {
                MessageBox.Show("You must enter an Ip Address!");
                return "null";
            }
            return (string)addressText.Text;
        }

        private void hostBtn_Click(object sender, EventArgs e) {
            string ip = getIpAddress();
            int port = getPortInteger();
            if (port == -1 || ip == "null") return;
            runMain(ip, port);
        }



    }

    public class Constants {

        //Server paramaters
        public const int NAME_SIZE = 40;
        public const int MAX_CHATS = 40;
        public const int BUFF_SIZE = 100000;
        public const int MAX_CUSTOM_NAME = 15;

        //Messages
        public const string CHAT_FULL = "Chat server is full! Maximum number of clients reached...";
        public const string WELCOME_MSG = "Welcome to chat!";

        //Commands
        public const string HELP = "/help";

    }



}
