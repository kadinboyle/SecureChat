using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server {
    public class ClientList {

        private Dictionary<String, Client> clients;
        private int idCount = 1000;
        private int numClients = 0;

        public ClientList() {
            clients = new Dictionary<String, Client>();
        }

        public Dictionary<String, Client> getDict(){
            return clients;
        }

        public void Add(Client newClient){
            clients.Add("C" + idCount++, newClient);
            numClients++;
        }

        //Remove by id
        //Overloaded
        public bool Remove(String id) {
            if (clients.Remove(id)) {
                numClients--;
                return true;
            }
            return false;
        }

        //remove by client
        public bool Remove(Client clientToRemove) {
            var item = clients.First(kvp => kvp.Value == clientToRemove);
            if (clients.Remove(item.Key)) {
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
