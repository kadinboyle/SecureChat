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
        private byte[] clientIdBytes;

        public Client() {

        }


        public Client(TcpClient client) {
            tcpClient = client;
            clientStream = tcpClient.GetStream();
            clientAddress = IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
            clientPort = (String)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
        }

        public void setId(string id) {
            this.clientIdStr = id;
            this.clientIdBytes = Encoding.UTF8.GetBytes(id);
        }

        public String ClientIdStr() {
            return this.clientIdStr;
        }

        public byte[] ClientIdBytes() {
            return this.clientIdBytes;
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
         * @return 0 if no data to send passed to buffer
         * @return -1 if stream unavailable to write to
         * @return -2 if stream no longer exists
         * @return 1 if OK and data written
         **/
        public int send(String msgToSend) {
            if (clientStream.CanWrite) {
                Byte[] sendBytes = Encoding.UTF8.GetBytes(msgToSend);
                try {
                    clientStream.Write(sendBytes, 0, sendBytes.Length);
                    return 1;
                } catch (ObjectDisposedException) {
                    return -2;
                } catch (ArgumentNullException) {
                    return 0;
                }
            }
            return -1;
        }

        public int send(Byte[] msgToSend) {
            if (clientStream.CanWrite) {
                try {
                    clientStream.Write(msgToSend, 0, msgToSend.Length);
                    return 1;
                } catch (ObjectDisposedException) {
                    return -2;
                } catch (ArgumentNullException) {
                    return 0;
                }
            }
            return -1;
        }

        public int buffSize() {
            return (int)tcpClient.ReceiveBufferSize;
        }

        public string receive() { 

            byte[] readBuffer = new byte[tcpClient.ReceiveBufferSize];
            StringBuilder myCompleteMessage = new StringBuilder();
            int numberOfBytesRead = 0;

            // Incoming message may be larger than the buffer size. 
            while(clientStream.DataAvailable){
                numberOfBytesRead = clientStream.Read(readBuffer, 0, readBuffer.Length);
                myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));

            }
            //while (clientStream.DataAvailable);

            // Print out the received message to the console.
            //Debug.WriteLine("You received the following message : " + myCompleteMessage);

            return myCompleteMessage.ToString();

            /*

            // Reads NetworkStream into a byte buffer. 
            byte[] bytes = new byte[tcpClient.ReceiveBufferSize];

            // Read can return anything from 0 to numBytesToRead.  
            // This method blocks until at least one byte is read.
            int read = clientStream.Read(bytes, 0, (int)tcpClient.ReceiveBufferSize);
            Debug.WriteLine("Receiving....." + read);
            if (read > 0) return bytes;
            // Returns the data received from the host to the console. 
            //string returndata = Encoding.UTF8.GetString(bytes);

            return null;
             * */
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
