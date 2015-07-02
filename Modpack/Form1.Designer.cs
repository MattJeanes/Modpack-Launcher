namespace Modpack
{
    partial class form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(form));
            this.launchbutton = new System.Windows.Forms.Button();
            this.console = new System.Windows.Forms.RichTextBox();
            this.progressbar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // launchbutton
            // 
            this.launchbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.launchbutton.Enabled = false;
            this.launchbutton.Location = new System.Drawing.Point(436, 302);
            this.launchbutton.Name = "launchbutton";
            this.launchbutton.Size = new System.Drawing.Size(94, 30);
            this.launchbutton.TabIndex = 0;
            this.launchbutton.Text = "Launch";
            this.launchbutton.UseVisualStyleBackColor = true;
            this.launchbutton.Click += new System.EventHandler(this.launchbutton_Click);
            // 
            // console
            // 
            this.console.BackColor = System.Drawing.SystemColors.Desktop;
            this.console.Dock = System.Windows.Forms.DockStyle.Top;
            this.console.Font = new System.Drawing.Font("Consolas", 10F);
            this.console.ForeColor = System.Drawing.SystemColors.Window;
            this.console.Location = new System.Drawing.Point(0, 0);
            this.console.Name = "console";
            this.console.ReadOnly = true;
            this.console.Size = new System.Drawing.Size(530, 296);
            this.console.TabIndex = 1;
            this.console.Text = "";
            // 
            // progressbar
            // 
            this.progressbar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.progressbar.Location = new System.Drawing.Point(0, 302);
            this.progressbar.Name = "progressbar";
            this.progressbar.Size = new System.Drawing.Size(430, 30);
            this.progressbar.TabIndex = 2;
            // 
            // form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 332);
            this.Controls.Add(this.progressbar);
            this.Controls.Add(this.console);
            this.Controls.Add(this.launchbutton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(500, 200);
            this.Name = "form";
            this.Text = "Modpack Launcher";
            this.Load += new System.EventHandler(this.form_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button launchbutton;
        private System.Windows.Forms.RichTextBox console;
        private System.Windows.Forms.ProgressBar progressbar;
    }
}

