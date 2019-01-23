using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TransactionServerModules
{
    public partial class Form2 : Form
    {
        List<Button> buttons;

        public Form2()
        {
            InitializeComponent();
        }

        public void init(object sender)
        {
            buttons = new List<Button>();
            buttons.Clear();
            Type type = sender.GetType();
            if ("Button" == type.Name)
            {
                Button button = sender as Button;
                button.Enabled = false;
                buttons.Add(button);
            }

        }

        public void uninit()
        {
            foreach (Button button in buttons)
            {
                button.Enabled = true;
            }
            buttons = null;
        }
    }
}
