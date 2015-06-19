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
using System.Collections;

namespace Server {


    public partial class ServerMain : Form {

        public ServerMain() {
            InitializeComponent();
            
        }

        //int[] fd_chatlist;
        List<TcpClient> clientlist = new List<TcpClient>();
        private int numClients = 0;

        public void sendToAll(string msg) {

        }

        public int runMain(int port) {
            //negate ip.

            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];

            IPAddress tempipAddress = IPAddress.Parse("127.0.0.1");

            TcpListener listener = new TcpListener(tempipAddress, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            listener.Start();
            //First connect
            if(listener.Pending())

            int exit = 0;
            while (exit == 0) {
                //if NOT pending new connection
                if (!listener.Pending()) {

                    //check all clients for input
                    for (int i = 0; i < numClients; i++) {
                        //if (clientlist[i].Available > 0) {
                        //read data
                        //send to all
                        //}
                    }
                }
                if(listener.Pending()) {
                    TcpClient newClient = listener.AcceptTcpClient();
                    MessageBox.Show("New client details: " + newClient.ToString());
                    //clientlist.Add(newClient);
                    newClient.Close();
                    exit = 1;
                }
                
            }
            listener.Stop();


            return 0;
        }

        

        static int enable_reuse_address(Socket socket) {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            return 0;
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
            //string ip = getIpAddress();
            int port = getPortInteger();
            //if (port == -1 || ip == "null") return;
            runMain(13000);
           // runMain(port);
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
