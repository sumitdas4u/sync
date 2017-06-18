using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SyncUtil
{
    /// <summary>
    /// Interaction logic for sett.xaml
    /// </summary>
    public partial class sett : Window
    {
        public sett()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.path = txtPath.Text.ToString();
            Properties.Settings.Default.Save();
            MessageBox.Show(txtPath.Text);
        }
    }
}
