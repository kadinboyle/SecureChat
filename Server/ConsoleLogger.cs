/**=================================================|
 # FileName: ConsoleLogger.cs (Server)
 # Author: Kadin Boyle
 # Date:   Last authored 15/07/2015
 # Description: Simple class to abstract logging text
 #              to a denoted 'console' text box.
 #
 #=================================================*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server{

    
    public class ConsoleLogger {

        private TextBox consoleBox;

        //Takes a TextBox object for constructor that is stored in private data field.
        public ConsoleLogger(TextBox _consoleBox) {
            this.consoleBox = _consoleBox;
            this.consoleBox.ScrollBars = ScrollBars.Vertical;
            //TODO: This will need to be removed later and delegate created for accessing combo box 
            //as ive done with the Textbox already.
            //-- System.Windows.Forms.Form.CheckForIllegalCrossThreadCalls = false;
        }

        public void log(String text) {
            consoleBox.AppendText("Server: " + text + Environment.NewLine);
        }

        public void log(byte[] bytes) {
            consoleBox.AppendText("Server: " + (Encoding.UTF8.GetString(bytes)) + Environment.NewLine);
        }


    }
}
