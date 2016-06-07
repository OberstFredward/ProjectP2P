using System;
using System.Media;
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

namespace ProjectP2P
{
    /// <summary>
    /// Interaktionslogik für SyncDialogWindow.xaml
    /// </summary>
    public partial class SyncDialogWindow : Window
    {
        private string id, iPv4, iPv6;
        public event EventHandler Yes;
        public event EventHandler No;


        public SyncDialogWindow(string[] acknowledge)
        {
            InitializeComponent();
            id = acknowledge[3];
            iPv4 = acknowledge[2];
            iPv6 = acknowledge[1];
            lblId.Content = id;
            lblIpv4.Content = iPv4;
            lblIpv6.Content = iPv6;
            SystemSounds.Exclamation.Play();
        }



        private void btnAblehnen_Click(object sender, RoutedEventArgs e)
        {
            No(this,new EventArgs());
        }

        private void btnAnnehmen_Click(object sender, RoutedEventArgs e)
        {
            Yes(this,new EventArgs());
        }
    }
}
