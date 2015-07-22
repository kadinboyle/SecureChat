using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server {
    public class ClientList {

        private ConcurrentDictionary<String, ServerClient> clientlist;
        private int idCount = 1000;
        private int numClients = 0;

        public ClientList() {
            clientlist = new ConcurrentDictionary<String, ServerClient>();
        }

        public ConcurrentDictionary<String, ServerClient> getDict() {
            return clientlist;
        }

        //this Dictionary<String, ServerClient>.ValueCollection valueCollectionValues
        public ICollection<ServerClient> ValuesD() {
            return clientlist.Values;
        }

        public void Add(ServerClient newClient) {
            newClient.ID = "C" + idCount;
            clientlist.GetOrAdd(newClient.ID, newClient);
            idCount++;
            numClients++;
        }

        //Remove by id NOT TO BE USED, AS IT CURRENTLY DOESNT SHUTDOWN THE CLIENT
        //Overloaded
        public bool Remove(String id) {
            ServerClient removed;
            if (clientlist.TryRemove(id, out removed)) {
                removed.Close();
                numClients--;
                return true;
            }
            return false;
        }

        //remove by client
        public bool Remove(ServerClient clientToRemove) {
            //var item = clients.First(kvp => kvp.Value == clientToRemove);
            ServerClient removed;
            if (clientlist.TryRemove(clientToRemove.ID, out removed)) {
                removed.Close();
                numClients--;
                return true;
            }
            return false;
        }

        public void ShutdownClients() {
            //Close all clients then remove them from the list
            foreach (var client in clientlist.Values) {
                client.Close();
            }
        }


        public ServerClient FindClientById(String id) {
            ServerClient found = null;
            clientlist.TryGetValue(id, out found);
            return found;
        }

        public int NumberClients {
            get { return this.numClients; }
            set { this.numClients = value; }
        }


    }
}
