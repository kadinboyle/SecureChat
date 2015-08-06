using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace ServerProgram {

    /// <summary>
    /// Represents a Server Client object used to manage connections to clients 
    /// connected to the server
    /// </summary>
    public class ServerClient {

        public TcpClient tcpClient;
        public NetworkStream clientStream;
        private IPAddress clientAddress;
        private IPAddress localAddress;
        private String clientPort;
        private String localPort;
        private String id;
        private bool isConnected;
        public byte[] buffer = new byte[10000];

        private ServerMessage EXIT_MSG = new ServerMessage("-exit", 1, "");

        public ServerClient() {
        }

        /// <summary>
        /// Constructs a new Client object to send/receive messages from clients
        /// </summary>
        /// <param name="client"></param>
        public ServerClient(TcpClient client) {
            tcpClient = client;
            clientStream = tcpClient.GetStream();
            clientAddress = IPAddress.Parse(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
            clientPort = (String)((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();
            localAddress = (((IPEndPoint)tcpClient.Client.LocalEndPoint)).Address;
            localPort = (((IPEndPoint)tcpClient.Client.LocalEndPoint)).Port.ToString();
            IsConnected = true;
        }

        /// <summary>
        /// Constructs a string with this Clients connection detail
        /// </summary>
        /// <returns>String of clients details</returns>
        public String ClientDetails() {
            return String.Format("Client [{0}]: Address: {1}:{2}", id, clientAddress, clientPort);
        }

        /// <summary>
        /// Returns the clients ID  
        /// </summary>
        public String ID {
            get { return this.id; }
            set { this.id = value; }
        }

        /// <summary>
        /// Returns the local port the client is connected to
        /// </summary>
        public String LocalPort {
            get { return this.localPort; }
            set { this.localPort = value; }
        }

        /// <summary>
        /// Returns the local address of this client
        /// </summary>
        public IPAddress LocalAddress {
            get { return this.localAddress; }
            set { this.localAddress = value; }
        }

        /// <summary>
        /// Returns the remote port of this client
        /// </summary>
        public String RemotePort {
            get { return this.clientPort; }
            set { clientPort = value; }
        }

        /// <summary>
        /// Returns the remote address of this client
        /// </summary>
        public IPAddress RemoteAddress {
            get { return this.clientAddress; }
            set { this.clientAddress = value; }
        }

        /// <summary>
        /// Returns True if the client object is currently connected or False if not
        /// </summary>
        public bool IsConnected {
            get { return isConnected; }
            set { isConnected = value; }
        }

        /// <summary>
        /// Initiates the shutdown of the connection for this Client.
        /// If the client hasnt initiated the disconnection, sends a message
        /// notifying them the server is terminating the connection
        /// </summary>
        /// <param name="clientInitiated">Specifies if the client requested the disconnection or not</param>
        public void Close(bool clientInitiated) {
            this.IsConnected = false;
            try {
                if (!clientInitiated) Send(EXIT_MSG.SerializeToBytes());
                clientStream.Close();
                clientStream.Dispose();
                return;
            } catch (Exception e) {
                Debug.WriteLine(e.ToString());
            }
            //if (tcpClient != null)
            tcpClient.Close();
        }

        /// <summary>
        /// Asynchronously sends a message to the remote host
        /// </summary>
        /// <param name="msgToSend">The message to send in byte[] format. Must be a Serialized ServerMessage</param>
        /// <returns>True if the message sent, False if it failed</returns>
        public bool Send(byte[] msgToSend) {
            if (clientStream.CanWrite) {
                try {
                    clientStream.WriteAsync(msgToSend, 0, msgToSend.Length);
                    return true;
                } catch (ObjectDisposedException) {
                    return false;
                    throw;
                } catch (ArgumentNullException) {
                    return false;
                    throw;
                } catch (Exception exc) {
                    Debug.WriteLine(exc.Message);
                }
            }
            return false;
        }

    }
}
