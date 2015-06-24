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

        public void send(String msgToSend) {
            if (clientStream.CanWrite) {
                Byte[] sendBytes = Encoding.UTF8.GetBytes(msgToSend);
                clientStream.Write(sendBytes, 0, sendBytes.Length);
            }
        }



    }
}
