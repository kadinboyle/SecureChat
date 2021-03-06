﻿/**=================================================|
 # FileName: ClientMain.cs (Client)
 # Author: Kadin Boyle
 # Date:   Last authored 11/08/2015
 #=================================================*/

using System;
using System.IO;
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
using ProtoBuf;
using System.Threading;
using System.Diagnostics;

namespace ClientProgram {
    public partial class ClientMain : Form {

        public static class Commands {
            public const string TERMINATE_CONN = "-exit";
            public const string CHANGE_NAME = "-name";
            public const string SAY = "-say";
            public const string WHISPER = "-whisper";
        }

        private volatile Client clientSelf;
        private IPAddress serverAddress;
        private ConsoleLogger console;
        private List<String> clientlist;
        public delegate void ObjectDelegate(object obj);
        public ObjectDelegate del_console;
        public ObjectDelegate del_clientlist;
        private static ManualResetEvent doneReading = new ManualResetEvent(false);
        private const int MAX_STR_LEN = 300;

        public ClientMain() {
            InitializeComponent();
            console = new ConsoleLogger(txtConsole);
            SetupEventHandlers();
            del_console = new ObjectDelegate(UpdateTextBox);
            del_clientlist = new ObjectDelegate(UpdateListBox);
            clientSelf = new Client();
            clientSelf.IsConnected = false;
        }

        /// <summary>
        /// Override the FormClosing so we can notify the server that we are disconnecting
        /// </summary>
        /// <param name="e">Event Arguments for Form Closing</param>
        protected override void OnFormClosing(FormClosingEventArgs e) {
            base.OnFormClosing(e);
            if (clientSelf.IsConnected) Shutdown();
        }

        /// <summary>
        /// Processes a message received from the Server.
        /// Deserializes the bytes into a ServerMessage object that is proccesed with
        /// a switch case tree, then procesed.
        /// </summary>
        /// <param name="msgReceived"></param>
        private void ProcessMessageReceived(byte[] msgReceived) {
            ServerMessage smsg = null;
            try {
                smsg = msgReceived.DeserializeFromBytes();
            } catch (Exception exc) {
                return;
            }
            if (smsg.noCommands == 0 || smsg.mainCommand.Equals("NULL")) {
                return;
            }
            String mainCommand = smsg.mainCommand;
            String secondCommand = "";
            int noCmds = smsg.noCommands;
            String payload = smsg.payload;
            if (noCmds == 2)
                secondCommand = smsg.secondCommand;

            switch (mainCommand) {
                case "-exit":
                    del_console.Invoke("Server is closing connection...");
                    Shutdown();
                    break;
                case "-newlist":
                    //We have received an update for our client list
                    del_clientlist.Invoke(payload);
                    break;
                case "-notifname":
                    MessageBox.Show("Your ID is: " + payload);
                    break;
                default:
                    del_console.Invoke(payload);
                    break;
            }
        }
        
        
        /// <summary>
        /// Process input passed from the GUI Form and parses data into a ServerMessage
        /// format, serializes it to bytes, and sends it to the server.
        /// </summary>
        /// <param name="commands">An Array of commands, either one or two, to be used in the ServerMessage</param>
        /// <param name="msg"></param>
        private void ParseMessage(String[] commands, String msg) {

            String mainCommand = commands[0];
            String secondCommand = "";
            if (commands.Length > 1)
                secondCommand = commands[1];
            ServerMessage servmsg = null;
            switch (mainCommand) {
                case Commands.SAY:
                    servmsg = new ServerMessage("-say", 1, msg);
                    break;

                case Commands.CHANGE_NAME:

                    break;

                case Commands.WHISPER:
                    servmsg = new ServerMessage("-whisper", secondCommand, 2, msg);
                    break;

                case "-getname":
                    servmsg = new ServerMessage("-getname", 1, msg);
                    break;

                default:
                    return;
            }

            byte[] toSend = servmsg.SerializeToBytes();

            try {
                if (!clientSelf.Send(toSend)) {
                    MessageBox.Show("Error sending message to Server!");
                }
            } catch (ObjectDisposedException exc) {
                MessageBox.Show(exc.ToString());
            } catch (ArgumentNullException exc) {
                MessageBox.Show(exc.ToString());
            }

        }


        //=============== BACKGROUND WORKER/THREADS ===============//
        //==================== ASYNC CALLBACKS ====================//

        /// <summary>
        /// Main method for the Background Worker running asynchronously to process communication
        /// between the client and the server.
        /// </summary>
        /// <param name="sender">Sender reference</param>
        /// <param name="e">Event Arguments passed from caller</param>
        private void bgWorker_mainLoop(object sender, DoWorkEventArgs e) {

            del_console.Invoke("Connected to " + clientSelf.RemoteAddress + " on port: " + clientSelf.RemotePort);

            //TODO: Think about use of property, cross thread 
            clientSelf.IsConnected = true;

            //Loop until client disonnects
            while (clientSelf.IsConnected) {
                DoBeginRead();
            }

        }

        /// <summary>
        /// Method automatically called when the background worker has finished execution
        /// </summary>
        /// <param name="sender">Sender refernce</param>
        /// <param name="e">Event arguments from caller</param>
        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show(e.Error.Message);
            }
            else {
                btnConnect.Enabled = true;
                //MessageBox.Show("Successfully Disonnected from Server!");
            }
        }

        /// <summary>
        /// Initiate the BeginRead Asynchronous operation for receiving data on the network stream
        /// from the server. Sets a ManualResetEvent "doneReading" then instructs the thread to wait 
        /// until set before continuing.
        /// </summary>
        private void DoBeginRead() {
            doneReading.Reset();
            if (clientSelf.IsConnected)

            clientSelf.clientStream.BeginRead(clientSelf.input_buffer, 0, 10000, new AsyncCallback(OnRead), clientSelf);
 
            doneReading.WaitOne();
        }

        /// <summary>
        /// OnRead Asynchronous callback for the DoBeginRead->stream BeginRead method. 
        /// Calls EndRead and passes the data to the ProcessMessageReceived method to be proccessed.
        /// </summary>
        /// <param name="ar">The Client object passed from the Asynchronous BeginRead operation</param>
        public void OnRead(IAsyncResult ar) {

            //Get Client object from async state and read data into buffer
            Client client = (Client)ar.AsyncState;
            int bytesread = 0;
            try {
                bytesread = client.clientStream.EndRead(ar);
            } catch (ObjectDisposedException) {
                //MessageBox.Show(exc.ToString());
                //Client has disconnected, so end async operations by returning
                doneReading.Set();
                return;
            }
            
            //Process the message and empty the Clients buffer (only take the amount read)
            if (bytesread > 0) {
                ProcessMessageReceived(client.input_buffer.Take(bytesread).ToArray());
            }

            Array.Clear(client.input_buffer, 0, client.input_buffer.Length);
            doneReading.Set();
            
        }


        /// <summary>
        /// Handles the Shutdown of the connection and cleans up.
        /// </summary>
        private void Shutdown() {
            if (clientSelf.IsConnected) {
                UpdateListBox(", ");
                clientSelf.Close();
                del_console.Invoke("Connection Terminated!");
                clientSelf.IsConnected = false; 
            }
        }

        //================ CROSS THREAD DELEGATES =================//
        //=========================================================//

        /// <summary>
        /// Cross Thread Delegate for updating the List Box of other clients on the Server.
        /// Because the Control was created on the original GUI Thread, this is neccessary for
        /// updating the list from the Background Worker Thread that processes communications.
        /// </summary>
        /// <param name="obj">The new list of clients</param>
        private void UpdateListBox(object obj) {

            //Check if Invoke required
            if (InvokeRequired) {
                Invoke(del_clientlist, obj);
                return;
            }
            String x = (String)obj;
            clientlist = x.Split(',').ToList();

            listBoxClients.DataSource = new BindingSource(clientlist, null);
        }

        /// <summary>
        /// Cross Thread Delegate for updating the main text area of the chat.
        /// Because the Control was created on the original GUI Thread, this is neccessary for
        /// updating the list from the Background Worker Thread that processes communications.
        /// </summary>
        /// <param name="obj">The data to display in the text area</param>
        private void UpdateTextBox(object obj) {

            //Check if Invoke required
            if (InvokeRequired) {
                Invoke(del_console, obj);
                return;
            }
            console.log((string)obj);
        }


        //=================== EVENT HANDLERS ======================//
        //=========================================================//

        /// <summary>
        /// Checks the information entered into the form is valid, and makes a connection to the
        /// server starting a background worker to process communications.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, EventArgs e) {

            //Check that the details the user entered are valid
            String addr = GetIpAddress();
            int port = GetPortInteger();
            if (addr.Equals("null") || port.Equals(-1)) return;


            try {
                clientSelf = new Client(new TcpClient(addr, port));
            } catch (ArgumentNullException) {
                MessageBox.Show("Invalid Hostname!", "Error");
                return;
            } catch (ArgumentOutOfRangeException) {
                MessageBox.Show("Port is out of range!", "Error");
                return;
            } catch (SocketException exc) {
                MessageBox.Show("Error connecting to host: \r\n\r\n" + exc.SocketErrorCode + ": " + exc.Message, "Error");
                return;
            }

            btnConnect.Enabled = false;

            // Set up background worker object & hook up handlers
            BackgroundWorker bgWorker;
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_mainLoop);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            // Launch background thread to loop for server response to input
            bgWorker.RunWorkerAsync();
            
        }

        /// <summary>
        /// Process input from text field and pass to ParseMessage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e) {
            String input = txtInput.Text.Trim();
            if (input.CountWords() < 1 || input.Length < 1) {
                return;
            }
            if (input.Length > 400) {
                MessageBox.Show("That message is too long! Maximum amount of characters is " + MAX_STR_LEN);
                return;
            }
            ParseMessage(new String[] { Commands.SAY }, txtInput.Text.Trim());
            txtInput.Text = "";
        }

        /// <summary>
        /// Sends a Private message to the person selected in the client list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnWhisper_Click(object sender, EventArgs e) {
            String targetClientId = (String)listBoxClients.SelectedValue;
            if (targetClientId == null || targetClientId.Length < 1) return;
            String input = txtInput.Text.Trim();
            if (input.CountWords() < 1 || input.Length < 1) {
                return;
            }
            del_console.Invoke("(PM) You to: (" + targetClientId + "): " + input);
            ParseMessage(new String[] { Commands.WHISPER, targetClientId }, input);
            txtInput.Text = "";
        }

        /// <summary>
        /// Shuts down the connection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, EventArgs e) {
            Shutdown();

        }

        /// <summary>
        /// Method setups event handlers for pressing enter in certain text boxes
        /// </summary>
        private void SetupEventHandlers() {
            //For pressing enter to send message
            this.txtInput.Enter += new EventHandler(txtInput_Enter);
            this.txtInput.Leave += new EventHandler(txtInput_Leave);

            //For pressing enter to connect when in Ip address or Port txt boxes
            EventHandler connect_enter = new EventHandler(serverParams_Enter);
            EventHandler connect_leave = new EventHandler(serverParams_Leave);
            this.txtAddress.Enter += connect_enter;
            this.txtAddress.Leave += connect_leave;
            this.txtPort.Enter += connect_enter;
            this.txtPort.Leave += connect_leave;
        }

        private void txtInput_Enter(object sender, EventArgs e) {
            ActiveForm.AcceptButton = btnSend;
        }

        private void txtInput_Leave(object sender, EventArgs e) {
            this.AcceptButton = null;
        }

        private void serverParams_Enter(object sender, EventArgs e) {
            this.AcceptButton = btnConnect;
        }

        private void serverParams_Leave(object sender, EventArgs e) {
            ActiveForm.AcceptButton = null;
        }

     
        //===================== FORM CHECKING =====================//
        //=========================================================//

        /// <summary>
        /// Gets the port number from the text field
        /// </summary>
        /// <returns></returns>
        public int GetPortInteger() {
            try {
                int port = Int32.Parse(txtPort.Text);
                return port;
            } catch (Exception) {
                MessageBox.Show("You must enter an integer between X and Y!", "Error");
                return -1;
            }
        }

        /// <summary>
        /// Gets the IP Address from the text field
        /// </summary>
        /// <returns></returns>
        public string GetIpAddress() {
            if ((txtAddress.Text).Length < 1) {
                MessageBox.Show("You must enter an Ip Address!", "Error");
                return "null";
            }
            return (String)txtAddress.Text;
        }

    }

    public static class Util {

        /// <summary>
        /// Counts the number of words in a given string
        /// </summary>
        /// <param name="s">The string to be counted</param>
        /// <returns>Number of words in the given string</returns>
        public static int CountWords(this String str) {
            return str.Split().Length;
        }
    }
}
