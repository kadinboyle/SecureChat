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

namespace ServerProgram {

    /// <summary>
    /// Represents the main Server class and GUI frontend.
    /// </summary>
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
        private int pServerPort;
        private static TcpListener tcpListener;
        private static volatile ClientList clientlist = new ClientList();

        //Delegate Declarations
        public delegate void ObjectDelegate(object obj);
        public static ObjectDelegate console_delegate;
        public static ObjectDelegate listbox_delegate;

        private static NATUPNPLib.UPnPNATClass upnpnat = new NATUPNPLib.UPnPNATClass();
        private static NATUPNPLib.IStaticPortMappingCollection mappings = upnpnat.StaticPortMappingCollection;


        public ServerMain() {
            InitializeComponent();
            console_obj = new ConsoleLogger(txtConsole);
            pServerRunning = false;
            clientlist = new ClientList();
            txtAddress.Text = ResolveAddress().ToString();
            txtPort.Text = "64500";
            SetupEventHandlers();
            console_delegate = new ObjectDelegate(UpdateTextBox);
            listbox_delegate = new ObjectDelegate(UpdateListBox);
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
            try {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                tcpListener.Start(Constants.MAX_BACKLOG);
            } catch (Exception exc) {
                MessageBox.Show(exc.Message);
                return;
            }
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            tcpListener.Start(Constants.MAX_BACKLOG);

            console_delegate.Invoke("I am listening for connections on " +
                                              IPAddress.Parse(((IPEndPoint)tcpListener.LocalEndpoint).Address.ToString()) +
                                               ":" + ((IPEndPoint)tcpListener.LocalEndpoint).Port.ToString());
            pServerRunning = true;
            while (pServerRunning) {
                //Check for new connection and then begin async reading operations for client
                BeginAcceptTcpClient();
            }
        }




        //============= COMMUNICATION AND PROCESSING ==============//
        //=========================================================//

        /// <summary>
        /// Processes a message received from a ServerClient and takes appropiate action depending
        /// on the contents of the ServerMessage. Takes a byte[] message and deserializes it back
        /// into ServerMessage format for examination.
        /// </summary>
        /// <param name="sender">The client the message was received from</param>
        /// <param name="msgReceived">The message recevied from the ServerClients buffer</param>
        private static void ProcessMessage(ServerClient sender, byte[] msgReceived) {
            ServerMessage serverMsg = msgReceived.DeserializeFromBytes();

            String mainCmd = serverMsg.mainCommand;
            String messageRecipientId = ""; 
            String payload = serverMsg.payload;

            //If the server message is a private message set messageRecipientId from it
            if (serverMsg.noCommands == 2 && mainCmd.Equals(Commands.WHISPER))
                messageRecipientId = serverMsg.secondCommand;

            switch (mainCmd) {
                case Commands.TERMINATE_CONN: //Sender has requested we terminate their connection
                    RemoveClient(sender.ID, false);
                    UpdateClientsList(); 
                    break;

                case Commands.SAY: //General chat message, forward on to all clients
                    String fullmsg = sender.ID + ": " + payload;
                    DistributeMessage(new ServerMessage("-say", 1, fullmsg));
                    break;

                case Commands.WHISPER: //Private message, only send to targeted client
                    if(serverMsg.noCommands  == 2)
                        SendPmToClient(clientlist.FindClientById(messageRecipientId), payload, sender.ID);
                    break;
                case "-getname":
                    ServerMessage servMsg = new ServerMessage("-notifname", 1, sender.ID);
                    SendServerMessageToClient(sender, servMsg);
                    break;
            }
        }

        /// <summary>
        /// Sends a private chat message to the targeted client.
        /// </summary>
        /// <param name="client">The client that will be the recipient of the message</param>
        /// <param name="msg">The chat message to be sent to the client</param>
        /// <param name="sendersId">The ID/name of the sender</param>
        private static void SendPmToClient(ServerClient client, string msg, string sendersId) {
            byte[] toSend = new ServerMessage("-say", 1, sendersId + "(Private Message): " + msg).SerializeToBytes();
            client.Send(toSend);
        }

        private static void SendServerMessageToClient(ServerClient recipient, ServerMessage servMsg) {
            byte[] toSend = servMsg.SerializeToBytes();
            recipient.Send(toSend);
        }

        /// <summary>
        /// Sends a ServerMessage to ALL connected clients.
        /// </summary>
        /// <param name="serv_msg">The ServerMessage to be sent</param>
        private static void DistributeMessage(ServerMessage serv_msg) {
            byte[] msg = serv_msg.SerializeToBytes();
            foreach (var client in clientlist.AllClients()) {
                try {
                    client.Send(msg);
                } catch (ObjectDisposedException) { // Stream is closed, remove this client
                    console_delegate.Invoke("Error occured sending message to: " + client.ID + ". Removing...");
                    RemoveClient(client.ID, true);
                } catch (ArgumentNullException) { //buffer invalid
                    MessageBox.Show("Invalid message attempting to be sent! Cancelling...", "Error");
                    return;
                };
            }
        }

        /// <summary>
        /// Adds a new ServerClient to the servera and notifies all clients of the new client
        /// </summary>
        /// <param name="client">The ServerClient to add to the server</param>
        private static void AddClient(ServerClient client) {
            clientlist.Add(client);
            UpdateClientsList();
            console_delegate.Invoke("New client joined :" + client.ID);
        }


        /// <summary>
        /// Removes a client from the Server based on a given ID. If the Client didnt initiate the termination
        /// then notifyClient must be set to true, so and exit message is sent notifying them to close their
        /// side of the connection. If the remove is successfull we notify all clients that has been removed.
        /// </summary>
        /// <param name="ID">The ID of the client that is to be removed</param>
        /// <param name="notifyClient">True if we wish to notify the client, False if they initiated the termination</param>
        /// <returns></returns>
        private static bool RemoveClient(string ID, bool notifyClient) {
            if (clientlist.Remove(ID, notifyClient)) {
                String leavemsg = ID + " leaves the chat...";
                console_delegate.Invoke(leavemsg);
                DistributeMessage(new ServerMessage("-say", 1, leavemsg));
                UpdateClientsList();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sends a ServerMessage out notifying all connected clients of an update to the list of clients
        /// on the server.
        /// </summary>
        private static void UpdateClientsList() {
            listbox_delegate.Invoke(null);
            String newlist = String.Join(",", clientlist.ClientIds());
            DistributeMessage(new ServerMessage("-newlist", 1, newlist));
        }

       /// <summary>
       /// Removes and Terminates all connected ServerClients and shuts their streams, then stops the servers
       /// TcpListener object.
       /// </summary>
        private void ShutdownServer() {
            mappings.Remove(pServerPort, "TCP");
            if (pServerRunning) {
                clientlist.ShutdownClients();
                tcpListener.Stop();
                pServerRunning = false;
                console_delegate.Invoke("Closing!...Connections Terminated... Server shutting down");
                UpdateListBox("");
                btnHost.Enabled = true;
            }
            else
                console_delegate.Invoke("Error Occured...");
        }

        // ================ ASYNC CALLBACK METHODS ================//
        //=========================================================//

        // Thread signal. 
        public static ManualResetEvent tcpClientConnected = new ManualResetEvent(false);

        /// <summary>
        /// Accept one Tcp Client Asynchronously
        /// </summary>
        /// <param name="listener">The TCP Listener object the server listens on for incoming connections</param>
        public static void BeginAcceptTcpClient() {
            if (!pServerRunning) return;
            // Set the event to nonsignaled state, Accept the connection, then till processed before continuing
            tcpClientConnected.Reset();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(OnAcceptTcpClientCallback), tcpListener);
            tcpClientConnected.WaitOne();
        }

        /// <summary>
        /// Proccess an accepted connection for a client asynchronously then begins asynchronous read operations
        /// for that client.
        /// </summary>
        /// <param name="ar">Asynchronous state of the listener</param>
        public static void OnAcceptTcpClientCallback(IAsyncResult ar) {

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
            
            //Add the client then start async reading
            AddClient(client);
            client.clientStream.BeginRead(client.buffer, 0, 10000, new AsyncCallback(OnReadClientCallback), client);
        }

        /// <summary>
        /// OnRead callback for each client. When the client has a message this method is called and processes it,
        /// before restarting another asynchronous read.
        /// </summary>
        /// <param name="ar">Asynchronous state of the client</param>
        public static void OnReadClientCallback(IAsyncResult ar) {

            //Get Client object from async state and read data into buffer
            ServerClient client = (ServerClient)ar.AsyncState;
            int bytesread = 0;
            try {
                bytesread = client.clientStream.EndRead(ar);
            } catch (ObjectDisposedException) {
                //Client has disconnected, so end async operations by returning
                return;
            }

            //Process the message and empty the Clients buffer (only take the amount read)
            if (bytesread > 0)
                ProcessMessage(client, client.buffer.Take(bytesread).ToArray());

            Array.Clear(client.buffer, 0, client.buffer.Length);

            //TODO: SET GLOBAL FOR BUFFER SIZE!!!
            //If the client is still connected after messasge processing above, continue, else end async reading operations
            if (client.IsConnected) {
                client.clientStream.BeginRead(client.buffer, 0, 10000, new AsyncCallback(OnReadClientCallback), client);
            }
            else return;

        }


        //=============== BACKGROUND WORKER/THREADS ===============//
        //=========================================================//

        /// <summary>
        /// Main Worker method, but actually calls RunMain() method with specified params.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bgWorker_serverLoop(object sender, DoWorkEventArgs e) {
            List<object> args = e.Argument as List<object>;
            RunMain((IPAddress)args[0], (int)args[1]);
        }

        /// <summary>
        /// Worker completed method. Called when the server is shutdown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show(e.Error.Message);
            }
            btnHost.Enabled = true;
            // Remove TCP forwarding for Port 80
   
        }


        //================ CROSS THREAD DELEGATES =================//
        //=========================================================//

        /// <summary>
        /// Delegate method for accessing the list box of clients between threads.
        /// </summary>
        /// <param name="obj">Not used</param>
        private void UpdateListBox(object obj) {

            //Check if Invoke required
            if (InvokeRequired) {
                Invoke(listbox_delegate, obj);
                return;
            }
            if (clientlist.NumberClients < 1) {
                listBoxClients.DataSource = new BindingSource("", null);
            }
            else {
                listBoxClients.DataSource = new BindingSource(clientlist.UnderlyingDictionary, null);
                listBoxClients.DisplayMember = "Key";
                listBoxClients.ValueMember = "Value";
            }
        }

        /// <summary>
        /// Delegate method for accessing the text box that displays server message between threads
        /// </summary>
        /// <param name="obj">Data to be dispalyed in the text area</param>
        private void UpdateTextBox(object obj) {

            //Check if Invoke required
            if (InvokeRequired) {
                Invoke(console_delegate, obj);
                return;
            }
            if (obj is byte[]) console_obj.log((byte[])obj);
            else console_obj.log((string)obj);
        }

        //=================== EVENT HANDLERS ======================//
        //=========================================================//

        /// <summary>
        /// Host Server action triggered when the user clicks "Host"
        /// Gets the Address and Port details from the text box and attempts to start the server on a 
        /// background worker thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHost_Click(object sender, EventArgs e) {
            IPAddress ip = GetIpAddress();
            int port = GetPortInteger();
            if (port == -1 || ip == null) return;


            pServerPort = port;
            // Here's an example of opening up TCP Port 80 to forward to a specific Computer on the Private Network
            //mappings.Add(80, "TCP", 80, "192.168.1.100", true, "Local Web Server");
            mappings.Add(port, "TCP", port, ip.ToString(), true, "Async Chat Server");



            // Set up background worker object & hook up handlers
            BackgroundWorker bgWorker;
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_serverLoop);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            // Launch background thread to loop for server response to input
            bgWorker.RunWorkerAsync(new List<object> { ip, port });
            btnHost.Enabled = false;
        }


        /// <summary>
        /// Stop server triggered when the user clicks "Stop". Calls Shutdown() on Server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, EventArgs e) {
            ShutdownServer();
        }

        /// <summary>
        /// Attempts to remove a client that has been selected in the client list box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRemoveClient_Click(object sender, EventArgs e) {
            if (clientlist.IsEmpty) return;
            try {
                if (!RemoveClient(((ServerClient)(listBoxClients.SelectedValue)).ID, false)) {
                    MessageBox.Show("Failed to remove Client!");
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Sets up event handelrs for the address and port text fields so user can press enter
        /// to host the server when the text fields are active.
        /// </summary>
        public void SetupEventHandlers() {
            EventHandler host_enter = new EventHandler(serverParams_Enter);
            EventHandler host_leave = new EventHandler(serverParams_Leave);
            this.txtAddress.Enter += host_enter;
            this.txtAddress.Leave += host_leave;
            this.txtPort.Enter += host_enter;
            this.txtPort.Leave += host_leave;
        }

        private void serverParams_Enter(object sender, EventArgs e) {
            this.AcceptButton = btnHost;
        }

        private void serverParams_Leave(object sender, EventArgs e) {
            this.AcceptButton = null;
        }

        //===================== FORM CHECKING =====================//
        //=========================================================//

        /// <summary>
        /// Retrieves the port number from the text field.
        /// </summary>
        /// <returns>The port to host the server on</returns>
        public int GetPortInteger() {
            try {
                int port = Int32.Parse(txtPort.Text);
                return port;
            } catch (Exception) {
                MessageBox.Show("You must enter an integer between X and Y!", "Error!");
                return -1;
            }
        }

        /// <summary>
        /// Retrieves the ip address from the text field.
        /// </summary>
        /// <returns>The address to host the server on</returns>
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

        /// <summary>
        /// Resolves an IPv4 address for the current host.
        /// </summary>
        /// <returns>The IPAddress of this computer</returns>
        private IPAddress ResolveAddress() {
            return Array.Find(Dns.GetHostEntry(string.Empty).AddressList,
                              a => a.AddressFamily == AddressFamily.InterNetwork) ?? IPAddress.Parse("127.0.0.1");
        }


    } //End ServerMain Class


    public static class Constants {

        //Server paramaters
        public const int MAX_BACKLOG = 20;
        public const int NAME_SIZE = 40;
        public const int MAX_CHATS = 10;
        public const int MAX_CUSTOM_NAME = 15;

        //Messages
        public const String CHAT_FULL = "Chat server is full! Maximum number of clients reached...";
        public const String WELCOME_MSG = "Welcome to chat!";

        //Commands
        public const String HELP = "/help";

    }

}
