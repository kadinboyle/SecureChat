using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Server {
    class ClientList {

        private static Dictionary<Client, String> clients;
        private static int idCount = 1000;
        private static int numClients = 0;

        public ClientList() {
            clients = new Dictionary<Client, String>();
        }

        public void add(Client newClient){
            clients.Add(newClient, "C" + idCount++);
            numClients++;
        }




    }
}
