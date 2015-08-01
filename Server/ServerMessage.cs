using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace ServerProgram{

    [ProtoContract]
    public class ServerMessage {

        [ProtoMember(1)]
        public int noCommands { get; set; }

        [ProtoMember(2)]
        public String mainCommand { get; set; }

        [ProtoMember(3)]
        public String secondCommand { get; set; }

        [ProtoMember(4)]
        public String payload { get; set; }

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

        public ServerMessage() {

        }

        public String ToString() {
            return "Main Command: " + mainCommand + Environment.NewLine +
                "Payload: " + payload;
        }

    }

    public static class SerializeUtils {
        public static byte[] SerializeToBytes<TData>(this TData msg) {
            using (var stream = new MemoryStream()) {
                Serializer.Serialize(stream, msg);
                return stream.ToArray();
            }
        }

        public static ServerMessage DeserializeFromBytes(this byte[] msg) {
            using (var stream = new MemoryStream(msg)) {
                return (ServerMessage)Serializer.Deserialize<ServerMessage>(stream);
            }
        }

        public static TData DeserializeFromBytes<TData>(this byte[] msg) {
            //byte[] b = Convert.FromBase64String(msg);
            using (var stream = new MemoryStream(msg)) {
                return (TData)Serializer.Deserialize<TData>(stream);
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
