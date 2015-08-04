using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace ClientProgram {

    /// <summary>
    /// Represents a ServerMessage. A serializeable object to be sent over the network
    /// using ProtoBuf implementation.
    /// </summary>
    [ProtoContract]
    public class ServerMessage {

        public ServerMessage() {
        }

        public ServerMessage(String mainCommand, int noCommands, String payload) {
            this.mainCommand = mainCommand;
            this.noCommands = noCommands;
            this.payload = payload;
        }

        public ServerMessage(String mainCommand, String secondCommand, int noCommands, String payload) {
            this.mainCommand = mainCommand;
            this.secondCommand = secondCommand;
            this.noCommands = noCommands;
            this.payload = payload;
        }


        /// <summary>
        /// Number of core commands in ServerMessage
        /// </summary>
        [ProtoMember(1)]
        public int noCommands { get; set; }

        /// <summary>
        /// The main command for the server message.
        /// </summary>
        [ProtoMember(2)]
        public String mainCommand { get; set; }

        /// <summary>
        /// The secondary command for the server message.
        /// </summary>
        [ProtoMember(3)]
        public String secondCommand { get; set; }

        /// <summary>
        /// The main data section of the server message.
        /// </summary>
        [ProtoMember(4)]
        public String payload { get; set; }

        /// <summary>
        /// Override ToString
        /// </summary>
        /// <returns>String representation of the message</returns>
        public override String ToString() {
            return "Main Command: " + mainCommand + Environment.NewLine +
                "Payload: " + payload;
        }

    }

    public static class SerializeUtils {

        /// <summary>
        /// Serializes a ServerMessage to byte format to be sent over network
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] SerializeToBytes<TData>(this TData msg) {
            using (var stream = new MemoryStream()) {
                Serializer.Serialize(stream, msg);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deseralizes bytes into server message format to be processed
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static ServerMessage DeserializeFromBytes(this byte[] msg) {
            using (var stream = new MemoryStream(msg)) {
                return (ServerMessage)Serializer.Deserialize<ServerMessage>(stream);
            }
        }

    }

    public static class Commands {
        public const string TERMINATE_CONN = "-exit";
        public const string CHANGE_NAME = "-name";
        public const string SAY = "-say";
        public const string WHISPER = "-whisper";
    }

}
