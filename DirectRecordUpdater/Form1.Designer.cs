namespace DirectRecordUpdater
{
    partial class Form1
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
            this.tableSelection = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.entityId = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.newValue = new System.Windows.Forms.TextBox();
            this.updateButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.updateResult = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // tableSelection
            // 
            this.tableSelection.FormattingEnabled = true;
            this.tableSelection.Items.AddRange(new object[] {
            "Party"});
            this.tableSelection.Location = new System.Drawing.Point(146, 36);
            this.tableSelection.Name = "tableSelection";
            this.tableSelection.Size = new System.Drawing.Size(294, 21);
            this.tableSelection.TabIndex = 0;
            this.tableSelection.Text = "Party";
            this.tableSelection.SelectedIndexChanged += new System.EventHandler(this.tableSelection_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(100, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Table";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // entityId
            // 
            this.entityId.Location = new System.Drawing.Point(146, 71);
            this.entityId.Name = "entityId";
            this.entityId.Size = new System.Drawing.Size(295, 20);
            this.entityId.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(89, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Entity Id";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(75, 109);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "New Value";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // newValue
            // 
            this.newValue.Location = new System.Drawing.Point(146, 106);
            this.newValue.Name = "newValue";
            this.newValue.Size = new System.Drawing.Size(295, 20);
            this.newValue.TabIndex = 4;
            this.newValue.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // updateButton
            // 
            this.updateButton.Location = new System.Drawing.Point(331, 153);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new System.Drawing.Size(112, 41);
            this.updateButton.TabIndex = 6;
            this.updateButton.Text = "update";
            this.updateButton.UseVisualStyleBackColor = true;
            this.updateButton.Click += new System.EventHandler(this.updateButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(75, 197);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Result";
            // 
            // updateResult
            // 
            this.updateResult.Location = new System.Drawing.Point(143, 200);
            this.updateResult.Multiline = true;
            this.updateResult.Name = "updateResult";
            this.updateResult.Size = new System.Drawing.Size(296, 160);
            this.updateResult.TabIndex = 8;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(522, 388);
            this.Controls.Add(this.updateResult);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.updateButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.newValue);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.entityId);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tableSelection);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox tableSelection;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox entityId;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox newValue;
        private System.Windows.Forms.Button updateButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox updateResult;
    }
}

