/**=================================================|
 # FileName: ClientList.cs (Server)
 # Author: Kadin Boyle
 # Date:   Last authored 02/08/2015
 #=================================================*/

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


namespace ServerProgram {

    /// <summary>
    /// Represents a ClientList object for the server to use to manage connected clients with
    /// methods for Adding, Removing, and Shutdown
    /// </summary>
    public class ClientList {

        private ConcurrentDictionary<String, ServerClient> clientdictionary;
        private int idCount = 1000;
       
        public ClientList() {
            clientdictionary = new ConcurrentDictionary<String, ServerClient>();
        }

        /// <summary>
        /// Gets the underlying ConcurrentDictionary object from ClientList wrapper class
        /// </summary>
        public ConcurrentDictionary<String, ServerClient> UnderlyingDictionary {
            get { return this.clientdictionary; }
        }

        /// <summary>
        /// Returns an Array of all currently connected client IDs
        /// </summary>
        /// <returns>Array of client ID's</returns>
        public String[] ClientIds() {
            return clientdictionary.Keys.ToArray();
        }

        /// <summary>
        /// Obtains iterable collection of all ServerClient objects contained within
        /// clientlist ConcurrentDictionary i.e all currently connected clients
        /// </summary>
        /// <returns></returns>
        public ICollection<ServerClient> AllClients() {
            return clientdictionary.Values;
        }

        /// <summary>
        /// Adds a new client to the client list and assigns them a unique id as a key
        /// </summary>
        /// <param name="newClient">The new ServerClient object to be added</param>
        public void Add(ServerClient newClient) {
            newClient.ID = "C" + idCount;
            clientdictionary.GetOrAdd(newClient.ID, newClient);
            idCount++;
        }

        /// <summary>
        /// Attempts to remove a client from the server, and shut it down gracefully,
        /// notifying the client they have been removed if they didnt initiate the 
        /// disconnection.
        /// </summary>
        /// <param name="id">ID of the client to be removed</param>
        /// <param name="notifyClient">Whether or not to notify the client they have been removed</param>
        /// <returns></returns>
        public bool Remove(String id, bool notifyClient) {
            ServerClient removed;
            if (clientdictionary.TryRemove(id, out removed)) {
                removed.Close(notifyClient);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes ALL currently connected clients from the server by calling the 
        /// Remove() method on each connected client.
        /// </summary>
        public void ShutdownClients() {
            //Close all clients then remove them from the list
            foreach (var client in clientdictionary.Values) {
                Remove(client.ID, false);
            }
        }

        /// <summary>
        /// Attempts to retrieve the ServerClient object associated with the given
        /// ID from the underying dictionary.
        /// </summary>
        /// <param name="id">The ID of the client to retrieve</param>
        /// <returns>The ServerClient that was retreived, or null if it couldnt be found</returns>
        public ServerClient FindClientById(String id) {
            ServerClient found = null;
            clientdictionary.TryGetValue(id, out found);
            return found;
        }

        /// <summary>
        /// Gets the number of clients currently connected
        /// <returns>The number of clients currently connected</returns>
        /// </summary>
        public int NumberClients {
            get { return clientdictionary.Count; }
        }

        /// <summary>
        /// Checks if the clientlist is empty or not
        /// <returns>True if empty, false if 1 or more clients</returns>
        /// </summary>
        public bool IsEmpty {
            get { return clientdictionary.IsEmpty; }
        }


    }
}
