using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MQTT_Connector
{
    public partial class Setup : Form
    {
        public bool to_save;
        public int row_i;
        public Setup()
        {
            InitializeComponent();
            to_save = false;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            to_save = true;
            this.Close();
        }

        private void button_ESC_Click(object sender, EventArgs e)
        {
            to_save = false;
            this.Close();
        }
    }
}
