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
        private IPAddress localAddress;
        private String clientPort;
        private String localPort;
        private String id;
        public byte[] buffer = new byte[65000];
        private ManualResetEvent doneReading;
        private StringBuilder strbuilder;
        private ServerMessage EXIT_MSG = new ServerMessage("-exit", 1, "EXIT");

        public ManualResetEvent DoneReading() {
            return doneReading;
        }

        public void SetEvent(ManualResetEvent mre) {
            doneReading = mre;
        }



        ~ServerClient() {
            //clientStream.Dispose();
        }

        public ServerClient() {

        }

        public ServerClient(TcpClient client) {
            tcpClient = client;
            clientStream = tcpClient.GetStream();
            clientAddress = IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
            clientPort = (String)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
            localAddress = (((IPEndPoint)tcpClient.Client.LocalEndPoint)).Address;
            localPort = (((IPEndPoint)tcpClient.Client.LocalEndPoint)).Port.ToString();
            strbuilder = new StringBuilder();
        }

        public void EmptyBuffer(){
            Array.Clear(buffer, 0, buffer.Length);
        }

        public NetworkStream GetStream() {
            return clientStream;
        }

        public String ClientDetails() {
            return String.Format("Client [{0}]: Address: {1}:{2}", id, clientAddress, clientPort);
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

        public void Close() {
            if (clientStream != null) {
                try {
                    Send(EXIT_MSG.SerializeToBytes());
                    clientStream.Close();
                    clientStream.Dispose();
                } catch (Exception e) {
                    Debug.WriteLine(e.ToString());
                }
            }

            if (tcpClient != null)
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
            if (clientStream.CanWrite) {
                Byte[] sendBytes = Encoding.ASCII.GetBytes(msgToSend);
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



        public bool Send(byte[] msgToSend) {
            if (clientStream.CanWrite) {
                try {
                    //clientStream.Write(msgToSend, 0, msgToSend.Length);
                    clientStream.WriteAsync(msgToSend, 0, msgToSend.Length);
                    Debug.WriteLine("Done writing " + ID);
                    return true;
                } catch (ObjectDisposedException) {
                    throw;
                } catch (ArgumentNullException) {
                    throw;
                }
            }
            return false;
        }

        //Not used anymore
        public string Receive() {

            byte[] readBuffer = new byte[tcpClient.ReceiveBufferSize];
            
            int numberOfBytesRead = 0;

            // Incoming message may be larger than the buffer size. 
            while (clientStream.DataAvailable) {
                numberOfBytesRead = clientStream.Read(readBuffer, 0, readBuffer.Length);
                strbuilder.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));

            }

            // Print out the received message to the console.
            //Debug.WriteLine("You received the following message : " + myCompleteMessage);

            string received = strbuilder.ToString();
            strbuilder.Clear();
            return received;
        }

        public bool HasMessage() {
            return clientStream.DataAvailable;
        }

    }
}
