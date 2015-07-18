﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Server {
    public class ServerClient{

        public TcpClient tcpClient;
        public NetworkStream clientStream;
        private IPAddress clientAddress;
        private String clientPort;
        private String clientIdStr;
        private byte[] clientIdBytes;

        public ServerClient() {

        }


        public ServerClient(TcpClient client) {
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

        public String ClientDetails() {
            return String.Format("Client [{0}]: Address: {1}:{2}", clientIdStr, clientAddress, clientPort);
        }

        public void Close() {
            send("-exit");
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
            //DEBUG
            Debug.WriteLine("Sending message");
            if (clientStream.CanWrite) {
                Byte[] sendBytes = Encoding.UTF8.GetBytes(msgToSend);
                try{
                    clientStream.Write(sendBytes, 0, sendBytes.Length);
                    return 1;
                }catch(ObjectDisposedException){
                    return -2;
                }catch(ArgumentNullException){
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


        public string receive() {

            byte[] readBuffer = new byte[tcpClient.ReceiveBufferSize];
            StringBuilder myCompleteMessage = new StringBuilder();
            int numberOfBytesRead = 0;

            // Incoming message may be larger than the buffer size. 
            while (clientStream.DataAvailable) {
                numberOfBytesRead = clientStream.Read(readBuffer, 0, readBuffer.Length);
                myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, numberOfBytesRead));

            }

            // Print out the received message to the console.
            //Debug.WriteLine("You received the following message : " + myCompleteMessage);

            return myCompleteMessage.ToString();
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