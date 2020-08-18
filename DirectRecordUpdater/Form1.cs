using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DirectRecordUpdater
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            try
            {
                UpdateData.Update(tableSelection.SelectedItem.ToString(), entityId.Text, newValue.Text);
                updateResult.Text = $"Success: {entityId.Text} set";
            }
            catch(Exception ex)
            {
                updateResult.Text = ex.Message;
            }


        }

        private void tableSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            tableSelection.Text = tableSelection.SelectedItem.ToString();
        }
    }
}
