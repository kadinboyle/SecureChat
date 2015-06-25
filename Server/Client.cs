using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Server {
    class Client {

        public static TcpClient tcpClient;
        public static NetworkStream clientStream;
        private static IPAddress clientAddress;
        private static String clientPort;
        private static String clientId;

        public Client(TcpClient client) {
            tcpClient = client;
            clientStream = tcpClient.GetStream();
            clientAddress = IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
            clientPort = (String)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
        }

        public String ClientDetails() {
            return String.Format("Client [{0}]: Address: {1}:{2}", clientId, clientAddress, clientPort);
        }

        public void Close() {
            clientStream.Close();
            tcpClient.Close();
        }

        public void send(String msgToSend) {
            if (clientStream.CanWrite) {
                Byte[] sendBytes = Encoding.UTF8.GetBytes(msgToSend);
                clientStream.Write(sendBytes, 0, sendBytes.Length);
            }
        }

        public byte[] receive() {
            if (clientStream.CanRead) {
                // Reads NetworkStream into a byte buffer. 
                byte[] bytes = new byte[tcpClient.ReceiveBufferSize];

                // Read can return anything from 0 to numBytesToRead.  
                // This method blocks until at least one byte is read.
                clientStream.Read(bytes, 0, (int)tcpClient.ReceiveBufferSize);

                // Returns the data received from the host to the console. 
                //string returndata = Encoding.UTF8.GetString(bytes);

                return bytes;
            }
            return null;
        }

        public string bytesToString(byte[] bytes) {
            return Encoding.UTF8.GetString(bytes);
        }





    }
}
