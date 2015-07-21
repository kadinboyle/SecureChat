using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace ClientProgram {

    public class Client {

        public static TcpClient tcpClient;
        public NetworkStream clientStream;
        private IPAddress clientAddress;
        private String clientPort;
        private String clientIdStr;
        private StringBuilder msgReceived;

        public Client() {

        }

        public Client(TcpClient client) {
            tcpClient = client;
            clientStream = tcpClient.GetStream();
            clientAddress = IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
            clientPort = (String)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
            msgReceived = new StringBuilder();
        }

        public bool CanWrite() {
            return clientStream.CanWrite;
        }

        public void setId(string id) {
            this.clientIdStr = id;
        }

        public String ClientIdStr() {
            return this.clientIdStr;
        }

        public String ClientDetails() {
            return String.Format("Client [{0}]: Address: {1}:{2}", clientIdStr, clientAddress, clientPort);
        }

        public void Close() {
            clientStream.Close();
            tcpClient.Close();
        }

        /**
         * Sending data (in bytes) to client via its NetworkStream
         * 
         * OVERLOADED
         * @param msgToSend The message to send in string format
         * 
         * @return true If message written successfully
         **/

        //TODO: UPDATE THIS TO THROW EXCEPTIONS.
        public bool send(String msgToSend) {
            if (clientStream.CanWrite) {
                Byte[] sendBytes = Encoding.UTF8.GetBytes(msgToSend);
                try {
                    clientStream.Write(sendBytes, 0, sendBytes.Length);
                    return true;
                } catch (ObjectDisposedException) {
                    throw;
                } catch (ArgumentNullException) {
                    throw;
                }
            }
            return false;
        }

        public string receive() { 

            byte[] readBuffer = new byte[tcpClient.ReceiveBufferSize];
            //StringBuilder myCompleteMessage = new StringBuilder();
            int numberOfBytesRead = 0;

            // Incoming message may be larger than the buffer size. 
            while(clientStream.DataAvailable){
                numberOfBytesRead = clientStream.Read(readBuffer, 0, readBuffer.Length);
                msgReceived.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));

            }
            string received = msgReceived.ToString();
            msgReceived.Clear();
            return received;
        }

        public bool hasMessage() {
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
