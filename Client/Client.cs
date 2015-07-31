using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;

namespace ClientProgram {

    public class Client {

        public static TcpClient tcpClient;
        public NetworkStream clientStream;
        private IPAddress clientAddress;
        private IPAddress localAddress;
        private String clientPort;
        private String localPort;
        private String id;
        private StringBuilder messageReceived;
        public byte[] input_buffer = new byte[10000];
        private volatile bool _isConnected;

        public Client() {

        }

        public Client(TcpClient client) {
            tcpClient = client;
            clientStream = tcpClient.GetStream();
            clientAddress = IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
            clientPort = (String)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
            localAddress = (((IPEndPoint)tcpClient.Client.LocalEndPoint)).Address;
            localPort = (((IPEndPoint)tcpClient.Client.LocalEndPoint)).Port.ToString();
            messageReceived = new StringBuilder();
        }

        public String ClientDetails() {
            return String.Format("Client [{0}]: Address: {1}:{2}", id, clientAddress, clientPort);
        }

        public bool CanWrite() {
            return clientStream.CanWrite;
        }

        public String ID {
            get { return this.id; }
            set { this.id = value; }
        }

        public String LocalPort {
            get { return this.localPort; }
            set { this.localPort = value; }
        }

        public IPAddress LocalAddress {
            get { return this.localAddress; }
            set { this.localAddress = value; }
        }

        public String RemotePort {
            get { return this.clientPort; }
            set { clientPort = value; }
        }

        public IPAddress RemoteAddress {
            get { return this.clientAddress; }
            set { this.clientAddress = value; }
        }

        //Notify Server we are closing nad release resources
        public void Close() {
            this.IsConnected = false;
            try {
                Send(new ServerMessage("-exit", 1, "Exit").SerializeToBytes());
                clientStream.Close();
                tcpClient.Close();
                clientStream.Dispose();
            } catch (Exception) {}
        }

        public bool Send(byte[] messageToSend) {
            //byte[] messageToSend = messageToSend.SerializeToBytes();
            if (clientStream.CanWrite) {
                //Byte[] sendBytes = Encoding.UTF8.GetBytes(msgToSend);
                try {
                    clientStream.Write(messageToSend, 0, messageToSend.Length);
                    return true;
                } catch (ObjectDisposedException) {
                    throw;
                } catch (ArgumentNullException) {
                    throw;
                }
            }
            return false;
        }
        
        public bool Send(String msgToSend) {
            if (clientStream.CanWrite) {
                //Byte[] sendBytes = Encoding.UTF8.GetBytes(msgToSend);
                try {
                    ServerMessage smsg = new ServerMessage("-say", 1, "WHAT THE FUCK you want biatch");
                    //MessageBox.Show(smsg.SerializeToString());
                    //clientStream.Write(sendBytes, 0, sendBytes.Length);
                    return true;
                } catch (ObjectDisposedException) {
                    throw;
                } catch (ArgumentNullException) {
                    throw;
                }
            }
            return false;
        }

        public byte[] Receive() { 

            byte[] readBuffer = new byte[tcpClient.ReceiveBufferSize];
            //StringBuilder myCompleteMessage = new StringBuilder();
            int numberOfBytesRead = 0;

            // Incoming message may be larger than the buffer size. 
            while(clientStream.DataAvailable){
                numberOfBytesRead = clientStream.Read(readBuffer, 0, readBuffer.Length);
                //messageReceived.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));

            }
            //string received = messageReceived.ToString();
           // messageReceived.Clear();
            return readBuffer.Take(numberOfBytesRead).ToArray();
        }

        public bool IsConnected {
            get { return _isConnected;  }
            set { _isConnected = value;  }
        }

        public bool HasMessage() {
            if (clientStream != null)
                return clientStream.DataAvailable;
            return false;
        }

    }

}
