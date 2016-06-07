using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class ChatWindow : Window
    {
        public event EventHandler<EventArgs<string>> SendText;
        private static string SavedText;
        public ChatWindow()
        {
            InitializeComponent();
            txbChat.Text = "";
            txbEingabe.Text = "";
            txbChat.Text = SavedText;
            MainWindow.ChatWindowIsOpened = true;
        }

        private void WpfChat_Closed(object sender, EventArgs e) //speichere in statischem Feld (Keine dauerhafte speicherung - der Text wird nur solange gehalten wie der P2P Client läuft)
        {
            MainWindow.ChatWindowIsOpened = false;
            if (txbChat.Text != "") SavedText = txbChat.Text;
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (txbEingabe.Text != "")
            {
                SendText(this, new EventArgs<string>(txbEingabe.Text));
                if(txbChat.Text == "") txbChat.Text += "<Du> " + txbEingabe.Text;
                else txbChat.Text += "\r\n<" + "<Du> " + "> " + txbEingabe.Text;
                txbEingabe.Text = "";
            }
        }

        public void RecieveText(string text)
        {
            if(txbChat.Text == "") txbChat.Text += "<" + MainWindow.partner.IPv4 + "> " + text;
            else txbChat.Text += "\r\n<" + MainWindow.partner.IPv4 + "> " + text;
        }

        private void txbEingabe_KeyDown(object sender, KeyEventArgs e) //Wenn EINGABE gedrückt wird - Event
        {
            if (e.Key == Key.Return) btnSend_Click(this,new RoutedEventArgs());
        }
    }

    public class EventArgs<T> : EventArgs
    {
        public T Data { get; set; }

        public EventArgs(T data)
        {
            Data = data;
        }
    }
}
