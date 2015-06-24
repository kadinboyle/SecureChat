using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server {
    public class ConsoleLogger {

        private TextBox consoleBox;

        public ConsoleLogger(TextBox _consoleBox) {
            this.consoleBox = _consoleBox;
        }



        public void WriteLine(String text) {
            consoleBox.AppendText(text + Environment.NewLine);
        }

        public void log(String text) {
            consoleBox.AppendText("Server: " + text + Environment.NewLine);

        }

    }
}
