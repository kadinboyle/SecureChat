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

        private volatile Client clientSelf;
        private IPAddress serverAddress;
        private ConsoleLogger console;
        private volatile int exit = 0;
        private List<String> clientlist;
        public delegate void ObjectDelegate(object obj);
        public ObjectDelegate del_console;
        public ObjectDelegate del_clientlist;
        private static ManualResetEvent doneReading = new ManualResetEvent(false);

        public ClientMain() {
            InitializeComponent();
            console = new ConsoleLogger(txtConsole);
            SetupEventHandlers();
            del_console = new ObjectDelegate(UpdateTextBox);
            del_clientlist = new ObjectDelegate(UpdateListBox);
            clientSelf = new Client();
            clientSelf.IsConnected = false;
        }

        //Override the FormClosing so we can notify the server that we are disconnecting
        protected override void OnFormClosing(FormClosingEventArgs e) {
            base.OnFormClosing(e);
            if (clientSelf.IsConnected) Shutdown();
        }

        private void DecryptMessage(string msg) {

        }

        //Parse into ServerMessage format, the serialize to byte[] and send
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
                default:
                    return;
            }

            byte[] toSend = servmsg.SerializeToBytes();

            try {
                if (!clientSelf.Send(toSend)) { }
                //MessageBox.Show("Error sending text!");
            } catch (ObjectDisposedException exc) {
                MessageBox.Show(exc.ToString());
            } catch (ArgumentNullException exc) {
                MessageBox.Show(exc.ToString());
            }

        }

        //Shutdown and Cleanup
        private void Shutdown() {
            if (clientSelf.IsConnected) {
                UpdateListBox(", ");
                clientSelf.Close();
                del_console.Invoke("Connection Terminated!");
                clientSelf.IsConnected = false;
            }
        }



        //=============== BACKGROUND WORKER/THREADS ===============//
        //=========================================================//


        private void DoBeginRead() {
            doneReading.Reset();
            if (clientSelf.IsConnected)
                clientSelf.clientStream.BeginRead(clientSelf.input_buffer, 0, 10000, new AsyncCallback(OnRead), clientSelf);
            doneReading.WaitOne();
        }

        public void OnRead(IAsyncResult ar) {

            //Get Client object from async state and read data into buffer
            Client client = (Client)ar.AsyncState;
            int bytesread = 0;
            try {
                bytesread = client.clientStream.EndRead(ar);
            } catch (ObjectDisposedException) {
                //Client has disconnected, so end async operations by returning
                return;
            }
            finally {
                doneReading.Set();
            }

            //Process the message and empty the Clients buffer (only take the amount read)
            if (bytesread > 0)
                ProcessMessageReceived(client.input_buffer.Take(bytesread).ToArray());

            Array.Clear(client.input_buffer, 0, client.input_buffer.Length);
        }

        //Processes a message received from the server
        private void ProcessMessageReceived(byte[] msgReceived) {
            ServerMessage smsg = msgReceived.DeserializeFromBytes();
            String mainCommand = smsg.mainCommand;
            String secondCommand = "";
            int noCmds = smsg.noCommands;
            String payload = smsg.payload;
            if (noCmds == 2)
                secondCommand = smsg.secondCommand;
            //TODO: Simplify the above into own method

            switch (mainCommand) {
                //TODO: Obviously this is fairly insecure
                case "-exit":
                    del_console.Invoke("Server is closing connection...");
                    //clientSelf.IsConnected = false;
                    Shutdown();
                    break;
                case "-newlist":
                    //We have received an update for our client list
                    del_clientlist.Invoke(payload);
                    break;
                default:
                    del_console.Invoke(payload);
                    break;
            }

        }



        //Main Server loop this one does all the work
        void bgWorker_mainLoop(object sender, DoWorkEventArgs e) {
            //TODO: Think about use of property, cross thread 
            clientSelf.IsConnected = true;

            //Loop until exit flag becomes 1
            while (clientSelf.IsConnected) {
                DoBeginRead();
            }

        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show(e.Error.Message);
            }
            else {
                //MessageBox.Show("Successfully Disonnected from Server!");
            }
        }

        //================ CROSS THREAD DELEGATES =================//
        //=========================================================//
        private void UpdateListBox(object obj) {
            //Check if control was created on a different thread.
            //If so, we need to call an Invoke method.
            if (InvokeRequired) {
                Invoke(del_clientlist, obj);
                return;
            }
            String x = (String)obj;
            clientlist = x.Split(',').ToList();

            //TODO: Fix empty list here...
            listBoxClients.DataSource = new BindingSource(clientlist, null);
        }

        private void UpdateTextBox(object obj) {

            if (InvokeRequired) { // Check if we need to switch threads to call on txt area.
                Invoke(del_console, obj);
                return;
            }
            console.log((string)obj);
        }


        //=================== EVENT HANDLERS ======================//
        //=========================================================//

        /****** Buttons ******/
        private void btnConnect_Click(object sender, EventArgs e) {

            //Check that the details the user entered are valid
            String addr = GetIpAddress();
            int port = GetPortInteger();
            if (addr.Equals("null") || port.Equals(-1)) return;

            try {
                clientSelf = new Client(new TcpClient(addr, port));
            } catch (ArgumentNullException) {
                MessageBox.Show("Invalid Hostname!");
                return;
            } catch (ArgumentOutOfRangeException) {
                MessageBox.Show("Port is out of range!");
                return;
            } catch (SocketException exc) {
                MessageBox.Show("Error connecting to host: \r\n\r\n" + exc.SocketErrorCode + ": " + exc.Message +
                    "\r\n\r\nPerhaps the server is not running or you have entered an invalid Address");
                return;
            }
            del_console.Invoke("Connected to " + clientSelf.RemoteAddress + " on port: ");
            clientSelf.IsConnected = true;
            btnConnect.Enabled = false;

            // Set up background worker object & hook up handlers
            BackgroundWorker bgWorker;
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_mainLoop);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            // Launch background thread to loop for server response to input
            bgWorker.RunWorkerAsync();
        }

        private void btnSend_Click(object sender, EventArgs e) {
            String input = txtInput.Text.Trim();
            if (CountWords(input) < 1 || input.Length < 1) {
                return;
            }
            ParseMessage(new String[] { Commands.SAY }, txtInput.Text.Trim());
            txtInput.Text = "";
        }

        private void btnWhisper_Click(object sender, EventArgs e) {
            String targetClientId = (String)listBoxClients.SelectedValue;
            String input = txtInput.Text.Trim();
            if (CountWords(input) < 1 || input.Length < 1) {
                return;
            }
            ParseMessage(new String[] { Commands.WHISPER, targetClientId }, input);
            txtInput.Text = "";
        }


        private void btnStop_Click(object sender, EventArgs e) {
            //clientSelf.IsConnected = false;
            Shutdown();

        }

        /****** Keys ******/

        //Method setups event handlers for pressing enter in certain text boxes
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

        public static int CountWords(string s) {
            return s.Split().Length;
        }

        //===================== FORM CHECKING =====================//
        //=========================================================//
        public int GetPortInteger() {
            try {
                int port = Int32.Parse(txtPort.Text);
                return port;
            } catch (Exception) {
                MessageBox.Show("You must enter an integer between X and Y!", "Error");
                return -1;
            }
        }

        public string GetIpAddress() {
            if ((txtAddress.Text).Length < 1) {
                MessageBox.Show("You must enter an Ip Address!", "Error");
                return "null";
            }
            return (string)txtAddress.Text;
        }

    }
}
