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

namespace ServerProgram{

    /// <summary>
    /// Represents an object that associates a given text box control
    /// as a "console" or named object
    /// </summary>
    public class ConsoleLogger {

        //Control references to the textbox
        private TextBox consoleBox;

        public ConsoleLogger(TextBox _consoleBox) {
            this.consoleBox = _consoleBox;
            this.consoleBox.ScrollBars = ScrollBars.Vertical;
        }
        
        /// <summary>
        /// Appends the text to the TextBox, when passed a string
        /// </summary>
        /// <param name="text">String text to appended</param>
        public void log(String text) {
            consoleBox.AppendText("Server: " + text + Environment.NewLine);
        }

        /// <summary>
        /// Appends the text to the TextBox, when passed a byte[]
        /// </summary>
        /// <param name="bytes"></param>
        public void log(byte[] bytes) {
            consoleBox.AppendText("Server: " + (Encoding.UTF8.GetString(bytes)) + Environment.NewLine);
        }


    }
}
