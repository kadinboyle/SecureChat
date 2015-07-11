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

namespace ClientProgram {
    public partial class ClientMain : Form {

        private Client clientSelf;
        private IPAddress serverAddress;
        private ConsoleLogger console;
        public int exit = 0;
        private BackgroundWorker bg_worker;
        public delegate void ObjectDelegate(object obj);
        public ObjectDelegate console_delegate;

        public ClientMain() {
            InitializeComponent();
            console = new ConsoleLogger(txtConsole);
            //For pressing enter to send message
            this.txtInput.Enter += new System.EventHandler(this.txtInput_Enter);
            this.txtInput.Leave += new System.EventHandler(this.txtInput_Leave);
        }


        private void updateTextBox(object obj) {
            if (InvokeRequired) {
                ObjectDelegate method = new ObjectDelegate(updateTextBox);
                // we then simply invoke it and return  

                Invoke(method, obj);
                return;
            }
            console.log((string)obj);
        }

        public string bytesToString(byte[] bytes) {
            return Encoding.UTF8.GetString(bytes);
        }

        void bgWorker_mainLoop(object sender, DoWorkEventArgs e) {
            while (exit == 0) {
                if (clientSelf.hasMessage()) {
                    string b = clientSelf.receive();
                    if(b != null)
                    console_delegate.Invoke(b);
                }
            }
            
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show(e.Error.Message);
            }
            else {
                clientSelf.Close();
            }
        }



        private void connectButton_Click(object sender, EventArgs e) {
            clientSelf = new Client(new TcpClient("127.0.0.1", 13000));
            IPAddress y = clientSelf.RemoteAddress();
            MessageBox.Show("Connected to " + y + " on port: ");

            console_delegate = new ObjectDelegate(updateTextBox);
            // Set up background worker object & hook up handlers
            BackgroundWorker bgWorker;
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_mainLoop);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            // Launch background thread to loop for server response to input
            bgWorker.RunWorkerAsync();
        }

        private void btnSend_Click(object sender, EventArgs e) {
            int success = clientSelf.send(txtInput.Text);
            if (success != 1) MessageBox.Show("Error sending text!");
            txtInput.Text = "";
        }

        private void txtInput_Enter(object sender, EventArgs e) {
            ActiveForm.AcceptButton = btnSend;
        }

        private void txtInput_Leave(object sender, EventArgs e) {
            ActiveForm.AcceptButton = null;
        }

        private void btnStop_Click(object sender, EventArgs e){
           clientSelf.send("-exit");
           exit = 1;
        }

    }
}
