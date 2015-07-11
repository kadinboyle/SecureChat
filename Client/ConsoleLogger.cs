﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientProgram{
    public class ConsoleLogger {

        private TextBox consoleBox;

        public ConsoleLogger(TextBox _consoleBox) {
            this.consoleBox = _consoleBox;
        }

        public void WriteLine(String text) {
            consoleBox.AppendText(text + Environment.NewLine);
        }

        public void log(String text) {
            consoleBox.AppendText(text);
            consoleBox.AppendText(Environment.NewLine); //Why on earth does this need to be on its own line?
        }

        public void log(byte[] bytes) {
            string text = (String)Encoding.UTF8.GetString(bytes);
            consoleBox.AppendText(text);
            consoleBox.AppendText(Environment.NewLine);
        }

    }
}