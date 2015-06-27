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

        private Client clientSelf = new Client();
        private IPAddress serverAddress;
        public int exit = 0;

        public ClientMain() {
            InitializeComponent();

            //For pressing enter to send message
            this.txtInput.Enter += new System.EventHandler(this.txtInput_Enter);
            this.txtInput.Leave += new System.EventHandler(this.txtInput_Leave);

            mainLoop();
        }

        private void mainLoop(){

            
            while(exit == 0){
                if(clientSelf.hasMessage()){
                    //txtConsole.AppendText(bytesToString(clientSelf.receive()) + "\n");
                }
            }
            clientSelf.Close();

        }

        public string bytesToString(byte[] bytes) {
            return Encoding.UTF8.GetString(bytes);
        }

        private void connectButton_Click(object sender, EventArgs e) {
            clientSelf = new Client(new TcpClient("127.0.0.1", 13000));
            IPAddress y = clientSelf.RemoteAddress();
            MessageBox.Show("Connected to " + y + " on port: ");
            //clientSelf.Close();
           

        }

        private void btnSend_Click(object sender, EventArgs e) {
            MessageBox.Show(txtInput.Text + " Clearing textbox...");
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
           exit = 1;
        }

    }
}
