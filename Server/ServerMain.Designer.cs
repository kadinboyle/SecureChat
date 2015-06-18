namespace Server
{
    partial class ServerMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.addressText = new System.Windows.Forms.TextBox();
            this.portText = new System.Windows.Forms.TextBox();
            this.hostBtn = new System.Windows.Forms.Button();
            this.consoleText = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(100, 50);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "IP Address";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(100, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Port";
            // 
            // addressText
            // 
            this.addressText.Location = new System.Drawing.Point(178, 47);
            this.addressText.Name = "addressText";
            this.addressText.Size = new System.Drawing.Size(168, 20);
            this.addressText.TabIndex = 2;
            // 
            // portText
            // 
            this.portText.Location = new System.Drawing.Point(178, 77);
            this.portText.Name = "portText";
            this.portText.Size = new System.Drawing.Size(168, 20);
            this.portText.TabIndex = 3;
            // 
            // hostBtn
            // 
            this.hostBtn.Location = new System.Drawing.Point(284, 118);
            this.hostBtn.Name = "hostBtn";
            this.hostBtn.Size = new System.Drawing.Size(61, 23);
            this.hostBtn.TabIndex = 4;
            this.hostBtn.Text = "Host";
            this.hostBtn.UseVisualStyleBackColor = true;
            this.hostBtn.Click += new System.EventHandler(this.hostBtn_Click);
            // 
            // consoleText
            // 
            this.consoleText.Location = new System.Drawing.Point(31, 181);
            this.consoleText.Multiline = true;
            this.consoleText.Name = "consoleText";
            this.consoleText.Size = new System.Drawing.Size(418, 88);
            this.consoleText.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(28, 162);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Console:";
            // 
            // ServerMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 281);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.consoleText);
            this.Controls.Add(this.hostBtn);
            this.Controls.Add(this.portText);
            this.Controls.Add(this.addressText);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ServerMain";
            this.Text = "Secure Server";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox addressText;
        private System.Windows.Forms.TextBox portText;
        private System.Windows.Forms.Button hostBtn;
        private System.Windows.Forms.TextBox consoleText;
        private System.Windows.Forms.Label label3;
    }
}

