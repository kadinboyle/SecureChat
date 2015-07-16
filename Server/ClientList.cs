using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server {
    public class ClientList {

        private ConcurrentDictionary<String, Client> clients;
        private int idCount = 1000;
        private int numClients = 0;

        public ClientList() {
            clients = new ConcurrentDictionary<String, Client>();
        }

        public ConcurrentDictionary<String, Client> getDict() {
            return clients;
        }

        public void Add(Client newClient) {
            newClient.setId("C" + idCount);
            clients.GetOrAdd(newClient.ClientIdStr(), newClient);
            idCount++;
            numClients++;
        }

        //Remove by id NOT TO BE USED, AS IT CURRENTLY DOESNT SHUTDOWN THE CLIENT
        //Overloaded
        public bool Remove(String id) {
            Client removed;
            if (clients.TryRemove(id, out removed)) {
                numClients--;
                return true;
            }
            return false;
        }

        //remove by client
        public bool Remove(Client clientToRemove) {
            //var item = clients.First(kvp => kvp.Value == clientToRemove);
            Client removed;
            if (clients.TryRemove(clientToRemove.ClientIdStr(), out removed)) {
                removed.Close();
                numClients--;
                return true;
            }
            return false;
        }

        public void ShutdownClients() {
            //Close all clients then remove them from the list
            foreach (var client in clients.Values) {
                client.Close();
            }
        }


        public Client findClientById(String id) {
            Client found = null;
            clients.TryGetValue(id, out found);
            return found;
        }

        public int getNoClients() {
            return numClients;
        }


    }
}
