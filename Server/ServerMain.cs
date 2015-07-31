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
        private static volatile Boolean pServerRunning;
        private IPAddress pServerAddress;
        private IPEndPoint pServerEndpoint;
        private String pServerPort;
        private TcpListener tcpListener;
        private static volatile ClientList clientlist = new ClientList();
        private static volatile ConcurrentDictionary<String, ServerClient> dictRef;
        private int numClients = 0;
        private int exit = 0;

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
        }

        //Override the FormClosing so we can notify all clients we are disconnecting...
        protected override void OnFormClosing(FormClosingEventArgs e) {
            base.OnFormClosing(e);
            if (pServerRunning) ShutdownServer();
        }


        //==================== MAIN SERVER LOOP ===================//
        //=========================================================//
        public void RunMain(IPAddress address, int port) {

            //Start tcp Listener object that will accept connections with a backlog specified.
            tcpListener = new TcpListener(address, port);
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            tcpListener.Start(Constants.MAX_BACKLOG);

            del_console.Invoke("I am listening for connections on " +
                                              IPAddress.Parse(((IPEndPoint)tcpListener.LocalEndpoint).Address.ToString()) +
                                               ":" + ((IPEndPoint)tcpListener.LocalEndpoint).Port.ToString());
            pServerRunning = true;
            while (pServerRunning) {
                //Check for new connection and then begin async reading operations for client
                DoBeginAcceptTcpClient(tcpListener);
            }
        }

        private static void ProcessMessage(ServerClient sender, byte[] msgReceived) {
            ServerMessage smsg = msgReceived.DeserializeFromBytes();

            String mainCommand = smsg.mainCommand;
            String messageRecipientId = "";
            int noCmds = smsg.noCommands;
            String payload = smsg.payload;
            if (noCmds == 2)
                messageRecipientId = smsg.secondCommand;

            switch (mainCommand) {
                case Commands.TERMINATE_CONN:
                    clientlist.Remove(sender.ID, true);
                    String leavemsg = sender.ID + " leaves the chat...";
                    UpdateClientsList();
                    del_console.Invoke(leavemsg);
                    DistributeMessage(new ServerMessage("-say", 1, leavemsg));
                    break;
                case Commands.SAY:
                    String fullmsg = sender.ID + ": " + payload;
                    DistributeMessage(new ServerMessage("-say", 1, fullmsg));
                    break;
                case Commands.WHISPER:
                    if (noCmds == 2)
                        SendToClient(clientlist.FindClientById(messageRecipientId), payload, sender.ID);
                    break;
            }
        }

        //============= COMMUNICATION AND PROCESSING ==============//
        //=========================================================//

        //Sends a message only to the specified client
        private static void SendToClient(ServerClient client, string msg, string sendersId) {
            byte[] toSend = new ServerMessage("-say", 1, sendersId + "(Private Message): " + msg).SerializeToBytes();
            client.Send(toSend);
        }

        //Used for sending important server message to all
        private static void DistributeMessage(ServerMessage serv_msg) {
            byte[] msg = serv_msg.SerializeToBytes();
            foreach (var client in clientlist.AllClients()) {
                try {
                    client.Send(msg);
                } catch (ObjectDisposedException o) { // Stream is closed
                    del_console.Invoke("Error occured sending message to: " + client.ID + ". Removing...");
                    RemoveClient(client.ID, true);
                } catch (ArgumentNullException exc) { //buffer invalid
                    MessageBox.Show(exc.ToString());
                };
            }
        }

        //Add the client then update our list and the clients
        private static void AddClient(ServerClient client) {
            clientlist.Add(client);
            UpdateClientsList();
            del_console.Invoke("Added new client");
        }


        // This method handles removing a client from the server
        // Removes it from the list AND initiates the clients 
        // shutdown methods whilst updating the listbox of clients
        private static bool RemoveClient(string ID, bool notifyClient) {
            if (clientlist.Remove(ID, notifyClient)) {
                UpdateClientsList();
                return true;
            }
            return false;
        }

        private static void UpdateClientsList() {
            del_list.Invoke(null);
            String newlist = String.Join(",", clientlist.ClientIds());
            DistributeMessage(new ServerMessage("-newlist", 1, newlist));
        }

        //Remove and terminate all client connections and streams, then stop the listener object
        private void ShutdownServer() {
            if (pServerRunning) {
                clientlist.ShutdownClients();
                tcpListener.Stop();
                pServerRunning = false;
                del_console.Invoke("Closing!...Connections Terminated... Server shutting down");
                btnHost.Enabled = true;
            }
            else
                del_console.Invoke("Error Occured...");
        }

        // ================ ASYNC CALLBACK METHODS ================//
        //=========================================================//

        // Thread signal. 
        public static ManualResetEvent tcpClientConnected = new ManualResetEvent(false);

        // Accept one client connection asynchronously. 
        public static void DoBeginAcceptTcpClient(TcpListener listener) {
            if (!pServerRunning) return;
            // Set the event to nonsignaled state, Accept the connection, then till processed before continuing
            tcpClientConnected.Reset();
            listener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), listener);
            tcpClientConnected.WaitOne();
        }

        // Process the client connection. 
        public static void DoAcceptTcpClientCallback(IAsyncResult ar) {

            ServerClient client;
            // Get the listener that handles the client request.
            TcpListener listener = (TcpListener)ar.AsyncState;
            try {
                client = new ServerClient(listener.EndAcceptTcpClient(ar));
            } catch (ObjectDisposedException) {
                return;
            }
            finally {
                tcpClientConnected.Set();
            }
            
            AddClient(client);
            client.clientStream.BeginRead(client.buffer, 0, 10000, new AsyncCallback(OnRead), client);
        }

        public static void OnRead(IAsyncResult ar) {
            //Set ServerClient object from async state and store number of bytes read,
            //whilst reading into buffer
            ServerClient client = (ServerClient)ar.AsyncState;
            int bytesread = 0;
            try {
                bytesread = client.clientStream.EndRead(ar);
            } catch (ObjectDisposedException) {
                //Stream disposed ignore and return. THIS WONT HAPPEN AS CHECK FOR CLIENT.ISCONNECTED works
                return;
            }

            //Process the message and empty the Clients buffer (only take the amount read)
            if (bytesread > 0)
                ProcessMessage(client, client.buffer.Take(bytesread).ToArray());

            Array.Clear(client.buffer, 0, client.buffer.Length);

            //TODO: SET GLOBAL FOR BUFFER SIZE!!!
            if (client.IsConnected) {
                client.clientStream.BeginRead(client.buffer, 0, 10000, new AsyncCallback(OnRead), client);
            }
            else {
                //Call return as the client is no longer connected
                return;
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
            MessageBox.Show("WORKER THREAD DONE!");
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
            //if (clientlist.NumberClients < 1) {

            //}
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
            btnHost.Enabled = false;
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
            ShutdownServer();
        }

        private void btnRemoveClient_Click(object sender, EventArgs e) {
            if (dictRef.IsEmpty) return;
            try {
                if (!RemoveClient(((ServerClient)(listBoxClients.SelectedValue)).ID, false)) {
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
        public const int MAX_BACKLOG = 20;
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
