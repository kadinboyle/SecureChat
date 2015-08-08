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

    /// <summary>
    /// Represents a Client object used to manage connections to the server
    /// </summary>
    public class Client {

        public static TcpClient tcpClient;
        public NetworkStream clientStream;
        private IPAddress clientAddress;
        private IPAddress localAddress;
        private String clientPort;
        private String localPort;
        private String id;

        //Buffer for reading from network stream
        public byte[] input_buffer = new byte[10000];
        private volatile bool _isConnected;

        public Client() {

        }

        /// <summary>
        /// Constructs a new Client object to send/receive messages from the server
        /// </summary>
        /// <param name="client">The TcpClient to use in this class for network communication</param>
        public Client(TcpClient client) {
            tcpClient = client;
            clientStream = tcpClient.GetStream();
            RemoteAddress = IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
            RemotePort = (String)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
            localAddress = (((IPEndPoint)tcpClient.Client.LocalEndPoint)).Address;
            localPort = (((IPEndPoint)tcpClient.Client.LocalEndPoint)).Port.ToString();
        }

        /// <summary>
        /// Constructs a String summarizing the clients details
        /// </summary>
        /// <returns>String of the clients details</returns>
        public String ClientDetails() {
            return String.Format("Client [{0}]: Address: {1}:{2}", id, clientAddress, clientPort);
        }

        /// <summary>
        /// Returns the ID associated with this Client
        /// </summary>
        public String ID {
            get { return this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Gets the local port number of this Client
        /// </summary>
        public String LocalPort {
            get { return this.localPort; }
            set { this.localPort = value; }
        }

        /// <summary>
        /// Gets the local IP Address of this Client
        /// </summary>
        public IPAddress LocalAddress {
            get { return this.localAddress; }
            set { this.localAddress = value; }
        }

        /// <summary>
        /// Gets the Remote Port this Client is connected to
        /// </summary>
        public String RemotePort {
            get { return this.clientPort; }
            set { clientPort = value; }
        }

        /// <summary>
        /// Gets the Remote Address this Client is connected to
        /// </summary>
        public IPAddress RemoteAddress {
            get { return this.clientAddress; }
            set { this.clientAddress = value; }
        }

        /// <summary>
        /// Shuts down the underying TCP Client object and closes the network stream 
        /// whilst notifying the server of disconnection.
        /// </summary>
        public void Close() {
            this.IsConnected = false;
            try {
                Send(new ServerMessage("-exit", 1, "Exit").SerializeToBytes());
                clientStream.Close();
                tcpClient.Close();
                clientStream.Dispose();
            } catch (Exception) {}
        }

        /// <summary>
        /// Sends a message to host the client is connected to.
        /// </summary>
        /// <param name="messageToSend">Message to send in byte[]</param>
        /// <returns></returns>
        public bool Send(byte[] messageToSend) {
            if (clientStream.CanWrite) {
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

        /// <summary>
        /// Returns true if the client is connected, false if disconnected
        /// </summary>
        public bool IsConnected {
            get { return _isConnected;  }
            set { _isConnected = value;  }
        }

    }

}
