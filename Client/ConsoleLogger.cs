/**=================================================|
 # FileName: ConsoleLogger.cs (Client)
 # Author: Kadin Boyle
 # Date:   Last authored 20/07/2015
 #=================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientProgram{

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
            consoleBox.AppendText(text);
            consoleBox.AppendText(Environment.NewLine); //Why on earth does this need to be on its own line?
        }

    }
}
