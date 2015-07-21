using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Server {
    public class ServerClient{

        public TcpClient tcpClient;
        public NetworkStream clientStream;
        private IPAddress clientAddress;
        private String clientPort;
        private String id;
        public byte[] buffer = new byte[65000];
        private ManualResetEvent doneReading;
        private StringBuilder msgReceived;

        public ManualResetEvent DoneReading() {
            return doneReading;
        }

        public void SetEvent(ManualResetEvent mre) {
            doneReading = mre;
        }

        ~ServerClient() {
            clientStream.Dispose();
        }

        public ServerClient() {

        }

        public ServerClient(String address, int port) {
            tcpClient = new TcpClient(address, port);
            clientStream = tcpClient.GetStream();
            clientAddress = IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
            clientPort = (String)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
        }

        public ServerClient(TcpClient client) {
            tcpClient = client;
            clientStream = tcpClient.GetStream();
            clientAddress = IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
            clientPort = (String)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
            msgReceived = new StringBuilder();
        }

        public void EmptyBuffer(){
            Array.Clear(buffer, 0, buffer.Length);
        }

        public NetworkStream GetStream() {
            return clientStream;
        }

        public string ID {
            get { 
                return this.id;
            }
            set {
                this.id = value;
            }
        }

        public String ClientDetails() {
            return String.Format("Client [{0}]: Address: {1}:{2}", id, clientAddress, clientPort);
        }

        public void Close() {
            Send("-exit");
            clientStream.Close();
            clientStream.Dispose();
            tcpClient.Close();
        }

        /**
         * Sending data (in bytes) to client via its NetworkStream
         * 
         * OVERLOADED
         * @param msgToSend The message to send in string format
         * 
         **/
        public bool Send(String msgToSend) {
            //DEBUG
            Debug.WriteLine("Sending message");
            if (clientStream.CanWrite) {
                Byte[] sendBytes = Encoding.UTF8.GetBytes(msgToSend);
                try{
                    clientStream.Write(sendBytes, 0, sendBytes.Length);
                    return true;
                }catch(ObjectDisposedException){
                    throw;
                }catch(ArgumentNullException){
                    throw;
                }
            }
            return false;
        }

        public string Receive() {

            byte[] readBuffer = new byte[tcpClient.ReceiveBufferSize];
            
            int numberOfBytesRead = 0;

            // Incoming message may be larger than the buffer size. 
            while (clientStream.DataAvailable) {
                numberOfBytesRead = clientStream.Read(readBuffer, 0, readBuffer.Length);
                msgReceived.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));

            }

            // Print out the received message to the console.
            //Debug.WriteLine("You received the following message : " + myCompleteMessage);

            string received = msgReceived.ToString();
            msgReceived.Clear();
            return received;
        }

        public bool HasMessage() {
            return clientStream.DataAvailable;
        }

        public IPAddress LocalAddress() {
            return this.clientAddress;
        }

        public String LocalPort() {
            return this.clientPort;
        }

        public IPAddress RemoteAddress() {
            return (((IPEndPoint)tcpClient.Client.RemoteEndPoint)).Address;
        }

        public String RemotePort() {
            return (((IPEndPoint)tcpClient.Client.LocalEndPoint)).Port.ToString();
        }

    }
}
