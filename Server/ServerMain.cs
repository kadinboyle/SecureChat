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
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;

namespace Server {


    public partial class ServerMain : Form {

        //TODO: Ensure all access to ConcurrentDictionary is done through clientlist class
        //TODO: ADD IN USE of """ USING """ statments
        //TODO: Move commands to shared class ServerMessage
        public static class Commands {
            public const string TERMINATE_CONN = "-exit";
            public const string CHANGE_NAME = "-name";
            public const string SAY = "-say";
            public const string WHISPER = "-whisper";
        }

        private static ConsoleLogger console_obj;
        private Boolean pServerRunning;
        private IPAddress pServerAddress;
        private IPEndPoint pServerEndpoint;
        private String pServerPort;
        private TcpListener tcpListener;
        private static ClientList clientlist = new ClientList();
        private static ConcurrentDictionary<String, ServerClient> dictRef;
        private int numClients = 0;
        private int exit = 0;
        private static StringBuilder messageReceived;

        public delegate void ObjectDelegate(object obj);
        public static ObjectDelegate del_console;
        public static ObjectDelegate del_list;


        public ServerMain() {
            InitializeComponent();
            console_obj = new ConsoleLogger(txtConsole);
            pServerRunning = false;
            clientlist = new ClientList();
            dictRef = clientlist.getDict();
            txtAddress.Text = ResolveAddress().ToString();
            txtPort.Text = "13000";
            SetupEventHandlers();
            //Set up our delegates for accessing console TextBox and Client ListBox cross thread
            del_console = new ObjectDelegate(UpdateTextBox);
            del_list = new ObjectDelegate(UpdateListBox);
            messageReceived = new StringBuilder();
        }

        //Override the FormClosing so we can notify all clients we are disconnecting...
        protected override void OnFormClosing(FormClosingEventArgs e) {
            base.OnFormClosing(e);
            ShutdownServer();
        }


        //==================== MAIN SERVER LOOP ===================//
        //=========================================================//
        public void RunMain(IPAddress address, int port) {

            tcpListener = new TcpListener(address, port);
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            tcpListener.Start(20);
            

            del_console.Invoke("I am listening for connections on " +
                                              IPAddress.Parse(((IPEndPoint)tcpListener.LocalEndpoint).Address.ToString()) +
                                               ":" + ((IPEndPoint)tcpListener.LocalEndpoint).Port.ToString());
            pServerRunning = true;
            while (exit == 0) {

                //Check for new connection
                if (tcpListener.Pending()) {
                    del_console.Invoke("New connection request...");
                    DoBeginAcceptTcpClient(tcpListener);
                }
            }//End while

            ShutdownServer();
        }

        private static void ProcessMessage(ServerClient sender, byte[] msgReceived) {
            ServerMessage smsg = msgReceived.DeserializeFromBytes();

            String mainCommand = smsg.mainCommand;
            String secondCommand = "";
            int noCmds = smsg.noCommands;
            String payload = smsg.payload;
            if (noCmds == 2)
                secondCommand = smsg.secondCommand;

            switch (mainCommand) {
                case Commands.TERMINATE_CONN: 
                    clientlist.Remove(sender.ID);
                    UpdateClientsList();
                    del_console.Invoke(sender.ID + " leaves the chat...");
                    break;
                case Commands.SAY:
                    SendToAll(payload, sender);
                    break;
                case Commands.WHISPER:
                    if(noCmds == 2)
                        SendToClient(clientlist.FindClientById(secondCommand), payload);                    
                    break;
            }
        }

        //============= COMMUNICATION AND PROCESSING ==============//
        //=========================================================//

        //Sends a message only to the specified client
        private static void SendToClient(ServerClient client, string msg) {

            byte[] toSend = new ServerMessage("-say", 1, client.ID +"(Private Message): " + msg).SerializeToBytes();
            client.Send(toSend);
            //client.Send(client.ID +"(Private Message): " + msg);
        }

        //Send a message to all clients on the server from a given sender
        private static void SendToAll(string msg, ServerClient sender) {
            String fullmsg = sender.ID +": " + msg;
            byte[] toSend = new ServerMessage("-say", 1, fullmsg).SerializeToBytes();
            foreach (var client in clientlist.ValuesD()) {
                try {
                    client.Send(toSend);
                } catch (ObjectDisposedException o) { // Stream is closed
                    MessageBox.Show(o.ToString() + Environment.NewLine + "-> Removing Client from server");
                    RemoveClient(client.ID);
                } catch (ArgumentNullException a) { //buffer invalid
                    MessageBox.Show(a.ToString());
                };
            }
        }

        private static void SendServerMessageAll(ServerMessage serv_msg) {
            byte[] msg = serv_msg.SerializeToBytes();
            foreach (var client in clientlist.ValuesD()) {
                try {
                    client.Send(msg);
                } catch (ArgumentNullException exc) { //buffer invalid
                    MessageBox.Show(exc.ToString());
                };
            }
        }


        // This method handles removing a client from the server
        // Removes it from the list AND initiates the clients 
        // shutdown methods whilst updating the listbox of clients
        private static bool RemoveClient(string ID) {
            if (clientlist.Remove(ID)) {
                UpdateClientsList();
                return true;
            }
            return false;
        }

        //Remove and terminate all client connections and streams, then stop the listener object
        private void ShutdownServer() {

            if (pServerRunning) {
                clientlist.ShutdownClients();
                tcpListener.Stop();
                pServerRunning = false;
                del_console.Invoke("Closing!...Connections Terminated... Server shutting down");
            }
            del_console.Invoke("Error Occured...");
        }

        // ================ ASYNC CALLBACK METHODS ================//
        //=========================================================//

        // Thread signal. 
        public static ManualResetEvent tcpClientConnected = new ManualResetEvent(false);

        // Accept one client connection asynchronously. 
        public static void DoBeginAcceptTcpClient(TcpListener listener) {

            // Set the event to nonsignaled state.
            tcpClientConnected.Reset();

            // Accept the connection.  
            listener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), listener);

            // Wait until a connection is made and processed before continuing
            tcpClientConnected.WaitOne();
        }

        private static void UpdateClientsList() {
            del_list.Invoke(null);
            String newlist = String.Join(",", clientlist.getDict().Keys.ToArray());
            SendServerMessageAll(new ServerMessage("-newlist", 1, newlist));
        }

        private static void AddClient(ServerClient client) {
            clientlist.Add(client);
            
            //Send new list to client
            UpdateClientsList();
            //String newlist = String.Join(",", clientlist.getDict().Keys.ToArray());
            //SendServerMessageAll(new ServerMessage("-newlist", 1, newlist));
            //MessageBox.Show(lista.ToString());
            del_console.Invoke("Added new client");
        }


        // Process the client connection. 
        public static void DoAcceptTcpClientCallback(IAsyncResult ar) {
            del_console.Invoke("New connection request...");

            // Get the listener that handles the client request.
            TcpListener listener = (TcpListener)ar.AsyncState;
            ServerClient client = new ServerClient(listener.EndAcceptTcpClient(ar));
            AddClient(client);

            // Signal the calling thread to continue.
            tcpClientConnected.Set();

            //Initiate callback method for reading from client
            DoBeginRead(client);
        }

        public static void DoBeginRead(ServerClient client) {
                ManualResetEvent cmre = new ManualResetEvent(false);
                client.SetEvent(cmre);
                client.clientStream.BeginRead(client.buffer, 0, 65000, new AsyncCallback(OnRead), client);
                client.DoneReading().WaitOne();
        }

        public static void OnRead(IAsyncResult ar) {

            //Await and Async??
            try {
                ServerClient client = (ServerClient)ar.AsyncState;
                if (!client.tcpClient.Connected) return;

                int bytesread = client.clientStream.EndRead(ar);
                //StringBuilder messageReceived = new StringBuilder();

                messageReceived.AppendFormat("{0}", Encoding.ASCII.GetString(client.buffer, 0, bytesread));
                //byte[] buffer = Encoding.ASCII.GetBytes(messageReceived.ToString());

                //Process the message and empty the Clients buffer (only take the amount read)
                if (messageReceived.Length > 0)
                    ProcessMessage(client, Encoding.ASCII.GetBytes(messageReceived.ToString()));
                messageReceived.Clear();
                client.EmptyBuffer();
                
                client.DoneReading().Set();   
                try {
                    client.clientStream.BeginRead(client.buffer, 0, 65000, new AsyncCallback(OnRead), client);
                } catch (Exception) { }
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
            }

        }


        //=============== BACKGROUND WORKER/THREADS ===============//
        //=========================================================//

        void bgWorker_serverLoop(object sender, DoWorkEventArgs e) {
            List<object> args = e.Argument as List<object>;
            RunMain((IPAddress)args[0], (int)args[1]);
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show(e.Error.Message);
            }
            //ShutdownServer();
        }

        //Resolves an IPv4 Address for this host
        private IPAddress ResolveAddress() {
            return Array.Find(Dns.GetHostEntry(string.Empty).AddressList,
                              a => a.AddressFamily == AddressFamily.InterNetwork) ?? IPAddress.Parse("127.0.0.1");
        }

        

        //================ CROSS THREAD DELEGATES =================//
        //=========================================================//
        private void UpdateListBox(object obj) {
            //Check if control was created on a different thread.
            //If so, we need to call an Invoke method.
            if (InvokeRequired) {
                Invoke(del_list, obj);
                return;
            }
            listBoxClients.DataSource = new BindingSource(dictRef, null);
            listBoxClients.DisplayMember = "Key";
            listBoxClients.ValueMember = "Value";
        }

        private void UpdateTextBox(object obj) {

            //Check if control was created on a different thread.
            //If so, we need to call an Invoke method.
            if (InvokeRequired) {
                Invoke(del_console, obj);
                return;
            }
            if (obj is byte[]) console_obj.log((byte[])obj);
            else console_obj.log((string)obj);
        }

        //=================== EVENT HANDLERS ======================//
        //=========================================================//
        private void hostBtn_Click(object sender, EventArgs e) {
            IPAddress ip = GetIpAddress();
            int port = GetPortInteger();
            if (port == -1 || ip == null) return;
            //btnHost.Enabled = false;
            //btnStop.Enabled = true;

            

            // Set up background worker object & hook up handlers
            BackgroundWorker bgWorker;
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_serverLoop);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            // Launch background thread to loop for server response to input
            bgWorker.RunWorkerAsync(new List<object> { ip, port });
        }

        private void btnStop_Click(object sender, EventArgs e) {
            if (pServerRunning == true) {
                exit = 1;
            }
        }

        private void btnRemoveClient_Click(object sender, EventArgs e) {
            if (dictRef.IsEmpty) return;
            try {
                if (!RemoveClient(((ServerClient)(listBoxClients.SelectedValue)).ID)) {
                    MessageBox.Show("Failed to remove Client!");
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }


 
        public void SetupEventHandlers() {
            EventHandler host_enter = new EventHandler(serverParams_Enter);
            EventHandler host_leave = new EventHandler(serverParams_Leave);
            this.txtAddress.Enter += host_enter;
            this.txtAddress.Leave += host_leave;
            this.txtPort.Enter += host_enter;
            this.txtPort.Leave += host_leave;
        }

        private void serverParams_Enter(object sender, EventArgs e) {
            ActiveForm.AcceptButton = btnHost;
        }
         
        private void serverParams_Leave(object sender, EventArgs e) {
            ActiveForm.AcceptButton = null;
        }

        //===================== FORM CHECKING =====================//
        //=========================================================//

        public int GetPortInteger() {
            try {
                int port = Int32.Parse(txtPort.Text);
                return port;
            } catch (Exception) {
                MessageBox.Show("You must enter an integer between X and Y!", "Error!");
                return -1;
            }
        }

        public IPAddress GetIpAddress() {
            if ((txtAddress.Text).Length < 1) {
                MessageBox.Show("You must enter an Ip Address!");
                return null;
            }
            IPAddress address = null;
            if (!IPAddress.TryParse(txtAddress.Text, out address)) {
                MessageBox.Show("Invalid IP Address!");
            }
            return address;
        }


        private static int enable_reuse_address(Socket socket) {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            return 0;
        }


    } //End ServerMain Class

    

    public static class Constants {

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

    public static class Utils {

        public static int CountWords(string s) {
            return s.Split().Length;
        }

    }



}
