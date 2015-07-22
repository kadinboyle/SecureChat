﻿using System;
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

namespace ClientProgram {
    public partial class ClientMain : Form {

        private Client clientSelf;
        private IPAddress serverAddress;
        private ConsoleLogger console;
        public int exit = 0;
        public delegate void ObjectDelegate(object obj);
        public ObjectDelegate del_console;

        public ClientMain() {
            InitializeComponent();
            console = new ConsoleLogger(txtConsole);
            SetupEventHandlers();
        }

        //Override the FormClosing so we can notify the server that we are disconnecting
        protected override void OnFormClosing(FormClosingEventArgs e) {
            base.OnFormClosing(e);
            if (clientSelf != null) {
                clientSelf.Send("-exit");
            }
        }


        private void DecryptMessage(string msg) {

        }

        private void ProcessMessage(string msg) {

            //ENCRYPTION HERE...
            //switch()
            try {
                if (!clientSelf.Send(msg))
                    MessageBox.Show("Error sending text!");
            } catch (ObjectDisposedException exc) {
                MessageBox.Show(exc.ToString());
            } catch (ArgumentNullException exc) {
                MessageBox.Show(exc.ToString());
            }

        }

        //Shutdown and Cleanup
        private void Shutdown() {
            if (clientSelf != null) {
                clientSelf.Send("-exit");
                clientSelf.Close();
                console.log("Connection Terminated...");
            }
            //pServerRunning = false;
        }

        //=============== BACKGROUND WORKER/THREADS ===============//
        //=========================================================//

        //Main Server loop this one does all the work
        void bgWorker_mainLoop(object sender, DoWorkEventArgs e) {

            //Loop until exit flag becomes 1
            while (exit == 0) {

                //Poll until we have a message
                if (clientSelf.HasMessage()) {

                    //Receive message then call it Invoke on the delegate
                    //to print it. (because txt box we are printing on is
                    //on the GUI's thread
                    string msg_received = clientSelf.Receive();
                    switch (msg_received) {
                        case "-exit":
                            console.log("Server is closing connection...");
                            exit = 1;
                            break;
                        case "placeholder":
                            break;
                        default:
                            del_console.Invoke(msg_received);
                            break;
                    }

                }
            }
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show(e.Error.Message);
            }
            else {
                Shutdown();
            }
        }

        //================ CROSS THREAD DELEGATES =================//
        //=========================================================//
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
        private void connectButton_Click(object sender, EventArgs e) {

            //Check that the details the user entered are valid
            String addr = GetIpAddress();
            int port = GetPortInteger();
            if (addr.Equals("null") || port.Equals(-1)) return;
            clientSelf = new Client(new TcpClient(addr, port));

            IPAddress y = clientSelf.RemoteAddress;
            MessageBox.Show("Connected to " + y + " on port: ");

            del_console = new ObjectDelegate(UpdateTextBox);
            // Set up background worker object & hook up handlers
            BackgroundWorker bgWorker;
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_mainLoop);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            // Launch background thread to loop for server response to input
            bgWorker.RunWorkerAsync();
        }

       

        private void btnSend_Click(object sender, EventArgs e) {
            if (CountWords(txtInput.Text.Trim()) < 1) {
                return;
            }              
            ProcessMessage("-say " + txtInput.Text.Trim());
            txtInput.Text = "";      
        }

        private void btnStop_Click(object sender, EventArgs e) {
            exit = 1;
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
            ActiveForm.AcceptButton = null;
        }

        private void serverParams_Enter(object sender, EventArgs e) {
            ActiveForm.AcceptButton = btnConnect;
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
                MessageBox.Show("You must enter an integer between X and Y!", "Error!");
                return -1;
            }
        }

        public string GetIpAddress() {
            if ((txtAddress.Text).Length < 1) {
                MessageBox.Show("You must enter an Ip Address!");
                return "null";
            }
            return (string)txtAddress.Text;
        }

    }
}
