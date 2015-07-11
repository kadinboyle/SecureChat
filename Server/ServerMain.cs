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

       

        private static ConsoleLogger console_obj;
        private Boolean pServerRunning;
        private IPAddress pServerAddress;
        private IPEndPoint pServerEndpoint;
        private String pServerPort;
        private TcpListener listener;
        private static ClientList clientlist = new ClientList();
        private static Dictionary<String, Client> dictRef;
        private int numClients = 0;
        
        private int exit = 0;
        

        public delegate void ObjectDelegate(object obj);
        public ObjectDelegate del;



        public ServerMain() {
            InitializeComponent();
            console_obj = new ConsoleLogger(txtConsole);
            pServerRunning = false;
            clientlist = new ClientList();
            dictRef = clientlist.getDict();

            
        }

   

        public void sendToAll(string msg, Client sender) {
            //console.Invoke(Utils.bytesToString(msg));
            string msgs = sender.ClientIdStr() + ": " + msg;
            //This might not work... 
            //perhaps use dictRef.Values OR clientlist.getDict() ???
            foreach(var client in clientlist.getDict().Values){
                client.send(msgs);
            }

        }

        //Remove and terminate all client connections and streams, then stop the listener object
        public void shutdownServer() {
            clientlist.ShutdownClients();
            listener.Stop();
            pServerRunning = false;
            console_obj.log("Closing!...Connections Terminated... Server shutting down");
        }

       

        public void runMain(IPAddress address, int port) {

           // ObjectDelegate del = (ObjectDelegate)delg; 

            //negate ip.
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            pServerAddress = ipHostInfo.AddressList[0];

            //Temp
            IPAddress tempipAddress = IPAddress.Parse("127.0.0.1");

            listener = new TcpListener(tempipAddress, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            listener.Start();
            pServerRunning = true;

            del.Invoke("I am listening for connections on " +
                                              IPAddress.Parse(((IPEndPoint)listener.LocalEndpoint).Address.ToString()) +
                                               "on port number " + ((IPEndPoint)listener.LocalEndpoint).Port.ToString());
            
            while (exit == 0) {
                //console.log("Loop");
                //Check for new connection
                if (listener.Pending()) {
                    del.Invoke("New connection request...");

                    Client newClient = new Client(listener.AcceptTcpClient());
                    //clientlist.Add(new Client(listener.AcceptTcpClient()));
                    clientlist.Add(newClient);
                    listBoxClients.DataSource = new BindingSource(dictRef, null);
                    listBoxClients.DisplayMember = "Key";
                    listBoxClients.ValueMember = "Value";
                    del.Invoke("Done adding new client");
                }

                //if NOT pending new connection, check all clients for input
                    foreach (var client in clientlist.getDict().Values) {
                        if (client.hasMessage()) {
                            string msgReceived = client.receive();
                            if (msgReceived.Equals(Constants.DISCONNECT_MSG)) {
                                del.Invoke(client.ClientIdStr() + " leaves the chatroom");
                                clientlist.Remove(client);
                                break;
                            }
                            else {
                                sendToAll(msgReceived, client);
                            }

                        }
                    }
            }
            shutdownServer();

        }

        private void updateTextBox(object obj) {
            if (InvokeRequired){  

                 // we then create the delegate again  
                 // if you've made it global then you won't need to do this  
                 ObjectDelegate method = new ObjectDelegate(updateTextBox);  
                 // we then simply invoke it and return  

                 Invoke(method, obj);  
                 return;  
          }
            if (obj is byte[]) console_obj.log((byte[])obj);
            else console_obj.log((string)obj);
        }

        void bgWorker_serverLoop(object sender, DoWorkEventArgs e) {
            List<object> args = e.Argument as List<object>;
            runMain((IPAddress)args[0], (int)args[1]);
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show(e.Error.Message);
            }
            else {
                shutdownServer();
            }
        }

        //TODO: Make delegates for accessing Form controls (console text box) from background worker threads

        private void hostBtn_Click(object sender, EventArgs e) {
            //string ip = getIpAddress();
            //int port = getPortInteger();
            //if (port == -1 || ip == "null") return;
            //btnHost.Enabled = false;
            //btnStop.Enabled = true;

            //DELETE:
            int port = 13000;
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            //List<object> args = new List<object> { ip, port };

            // first thing we do is create a delegate pointing to update method  

            del = new ObjectDelegate(updateTextBox);  

            // Set up background worker object & hook up handlers
            BackgroundWorker bgWorker;
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_serverLoop);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            // Launch background thread to loop for server response to input
            bgWorker.RunWorkerAsync(new List<object> { ip, port});
      

        }

        private void btnStop_Click(object sender, EventArgs e) {
            if (pServerRunning == true) {
                //shutdownServer();
                exit = 1;
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

        private void btnRemoveClient_Click(object sender, EventArgs e) {

            try {
               
                //KEEP THIS FOR LATER: listBoxClients.DataSourceChanged
                clientlist.Remove(((Client)(listBoxClients.SelectedValue)).ClientIdStr());
                MessageBox.Show("WTF BRUH 222");
                listBoxClients.Items.Remove(listBoxClients.SelectedItem);
                
                listBoxClients.DataSource = listBoxClients;
                MessageBox.Show("WTF BRUH 333");

               
            }
            catch(Exception){

            }

            //listBox1.DataSource = null;
            //listBoxClients.DataSource = _items;
        }





    }

    public class Constants {

        public static byte[] DISCONNECT_MSGB = Encoding.ASCII.GetBytes("-exit");
        public static string DISCONNECT_MSG = "-exit";

        //Server paramaters
        public const int NAME_SIZE = 40;
        public const int MAX_CHATS = 40;
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

        public static byte[] stringToBytes(string str) {
            return Encoding.UTF8.GetBytes(str);
        }

        public static bool Compare(byte[] b1, byte[] b2) {
            MessageBox.Show("Comparison being made...");
            return Encoding.ASCII.GetString(b1) == Encoding.ASCII.GetString(b2);
        }

        public static byte[] Combine(byte[] first, byte[] second) {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

    }



}
