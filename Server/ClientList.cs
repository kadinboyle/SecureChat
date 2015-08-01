﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


namespace ServerProgram {


    public class ClientList {

        private ConcurrentDictionary<String, ServerClient> clientlist;
        private int idCount = 1000;
       
        public ClientList() {
            clientlist = new ConcurrentDictionary<String, ServerClient>();
        }

        public ConcurrentDictionary<String, ServerClient> UnderlyingDictionary {
            get { return this.clientlist; }
        }

        public String[] ClientIds() {
            return clientlist.Keys.ToArray();
        }

        //this Dictionary<String, ServerClient>.ValueCollection valueCollectionValues
        public ICollection<ServerClient> AllClients() {
            return clientlist.Values;
        }

        public void Add(ServerClient newClient) {
            newClient.ID = "C" + idCount;
            clientlist.GetOrAdd(newClient.ID, newClient);
            idCount++;
        }

        //Removes a client from this clientlist and shuts it down gracefully
        public bool Remove(String id, bool notifyClient) {
            ServerClient removed;
            if (clientlist.TryRemove(id, out removed)) {
                removed.Close(notifyClient);
                return true;
            }
            return false;
        }

        //Removes all Clients from this list gracefully
        public void ShutdownClients() {
            //Close all clients then remove them from the list
            foreach (var client in clientlist.Values) {
                Remove(client.ID, false);
            }
        }

        //Find the client object associated with a given ID.
        public ServerClient FindClientById(String id) {
            ServerClient found = null;
            clientlist.TryGetValue(id, out found);
            return found;
        }

        public int NumberClients {
            get { return clientlist.Count; }
        }

        public bool IsEmpty {
            get { return clientlist.IsEmpty; }
        }


    }
}
