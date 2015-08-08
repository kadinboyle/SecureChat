using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

namespace ServerProgram {

    static class ServerProgram {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string resource1 = "ServerProgram.lib.protobuf-net.dll";
            string resource2 = "ServerProgram.lib.Interop.NATUPNPLib.dll";
            EmbeddedAssembly.Load(resource1, "protobuf-net.dll");
            EmbeddedAssembly.Load(resource2, "Interop.NATUPNPLib.dll");



            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            //Application.Run(new ServerMain(server));
            Application.Run(new ServerMain());
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            return EmbeddedAssembly.Get(args.Name);
        }
    }
}
