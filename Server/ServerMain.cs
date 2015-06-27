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
using System.Threading;

namespace Server {


    public partial class ServerMain : Form {

        private static ConsoleLogger console;
        private Boolean pServerRunning;
        private IPAddress pServerAddress;
        private IPEndPoint pServerEndpoint;
        private String pServerPort;
        private TcpListener listener;
        private static ClientList clientlist = new ClientList();
        private static Dictionary<String, Client> dictRef;
        private int numClients = 0;

        private int exit = 0;


        public ServerMain() {
            InitializeComponent();
            console = new ConsoleLogger(txtConsole);
            pServerRunning = false;
            btnStop.Enabled = false;
            clientlist = new ClientList();
            dictRef = clientlist.getDict();
        }

   

        public void sendToAll(byte[] msg) {
            console.log(Utils.bytesToString(msg));
            //This might not work... 
            //perhaps use dictRef.Values OR clientlist.getDict() ???
            foreach(var client in clientlist.getDict().Values){
                client.send(msg);
            }

        }

        public void shutdownServer() {
            //Remove all clients and shutdown streams/TCPClient Objects
            clientlist.ShutdownClients();

            //Shutdown listener
            listener.Stop();

            //Set server status to not running
            pServerRunning = false;
            console.log("Closing!...Connections Terminated... Server shutting down");
        }

       

        public int runMain(int port) {

            //negate ip.

            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            pServerAddress = ipHostInfo.AddressList[0];

            //Temp
            IPAddress tempipAddress = IPAddress.Parse("127.0.0.1");

            listener = new TcpListener(tempipAddress, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            listener.Start();
            pServerRunning = true;

            console.log("I am listening for connections on " +
                                              IPAddress.Parse(((IPEndPoint)listener.LocalEndpoint).Address.ToString()) +
                                               "on port number " + ((IPEndPoint)listener.LocalEndpoint).Port.ToString());
            
            int pRestrictTwo = 1;
            while (exit == 0) {
                //console.log("Loop");
                //Check for new connection
                if (listener.Pending() && pRestrictTwo == 1) {
                    console.log("new conn...");

                    //Client newClient = new Client(listener.AcceptTcpClient());
                    //clientlist.Add(newClient);
                    //SHORTCUT:
                    clientlist.Add(new Client(listener.AcceptTcpClient()));
                    console.log("Done adding new client");
                }

                //if NOT pending new connection
                //if (!listener.Pending()) {
                    foreach (var client in clientlist.getDict().Values) {
                        //MessageBox.Show("-->" + client.ClientDetails());
                        if (client.hasMessage()) {
                            console.log(client.ClientDetails() + "Wants to send a message!");
                            sendToAll(client.receive());
                            //console.log(Utils.bytesToString(client.receive()));
                            //sendToAll(client.receive());
                        }
                    }
                //}
            }


            shutdownServer();
            return 0;
        }

        private void hostBtn_Click(object sender, EventArgs e) {
            //string ip = getIpAddress();
            //int port = getPortInteger();
            //if (port == -1 || ip == "null") return;
            btnHost.Enabled = false;
            btnStop.Enabled = true;
            runMain(13000);
            // runMain(port);

        }

        private void btnStop_Click(object sender, EventArgs e) {
            MessageBox.Show("GGGEE");
            if (btnStop.Enabled == true && pServerRunning == true) {

            }
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

    public class Utils {
        public static string bytesToString(byte[] bytes) {
            return Encoding.UTF8.GetString(bytes);
        }
    }



}
