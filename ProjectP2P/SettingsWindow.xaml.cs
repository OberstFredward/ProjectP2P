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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;

namespace ProjectP2P
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        internal event EventHandler<EventArgs> StartStopListener;
        private FolderBrowserDialog selectFolder;
        public SettingsWindow()
        {
            Debug.WriteLine("Erstelle SettingsWindow Objekt mit übergebenen MainWindow.settings");
            InitializeComponent();
            //geladene Einstellungen anzeigen
            if (MainWindow.settings.enableLocalOnly) rbtnLocal.IsChecked = true;
            else if (!MainWindow.settings.enableLocalOnly) rbtnExtern.IsChecked = true;
            CheckBoxEnableUDP.IsChecked = MainWindow.settings.enableUDP;
            CheckBoxListen.IsChecked = MainWindow.settings.listen;
            if (MainWindow.settings.SendingProtocol == 0) comboBoxProtocol.SelectedItem = comboBoxProtocol.Items[0];
            else comboBoxProtocol.SelectedItem = comboBoxProtocol.Items[1];
            txbPort.Text = Convert.ToString(MainWindow.settings.ListenPort);
            txbPath.Text = MainWindow.settings.path;
        }

        private void btnSpeichern_Click(object sender, RoutedEventArgs e)
        {
            bool failed = true;
            if (rbtnLocal.IsChecked.Value) MainWindow.settings.enableLocalOnly = true;
            else if (rbtnExtern.IsChecked.Value) MainWindow.settings.enableLocalOnly = false;
            MainWindow.settings.enableUDP = CheckBoxEnableUDP.IsChecked.Value;
            MainWindow.settings.listen = CheckBoxListen.IsChecked.Value;
            MainWindow.settings.path = txbPath.Text;
            if (comboBoxProtocol.SelectedIndex == 0) MainWindow.settings.SendingProtocol = 0;
            else MainWindow.settings.SendingProtocol = 1;
            try
            {
                int port = Convert.ToInt32(txbPort.Text);
                if (port >= 49152 && port <= 65535)
                {
                    MainWindow.settings.ListenPort = Convert.ToInt32(txbPort.Text);
                    failed = false;
                }
                else
                {
                    Debug.WriteLine("Error: Port entspricht nicht der Syntax");
                    failed = true;
                }
            }
            catch (Exception)
            {
                failed = true;
            }
            if (!failed)
            {
                MainWindow.settings.SaveSettings();
                StartStopListener(this,new EventArgs());
                Close();
            }
            else MessageBox.Show("Bitte geben Sie einen gültigen Port an\nBitte wählen Sie einen unreservierten Port.\nUnreserviert sind Port 49152 - 65535");
        }

        private void CheckBoxEnableLocalOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            if (MainWindow.FirstTimeDisableLocalOnly)
            {
                MessageBoxResult result = MessageBox.Show("Um das Empfangen außerhalb diese Netzwerk zu ermöglichen,\nmuss der entsprechende Port ["+Convert.ToString(MainWindow.settings.ListenPort)+"] mit dem entsprechendem\nProtokoll im Router freigeschaltet sein.", "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        rbtnLocal.IsChecked = true;
                        break;
                    case MessageBoxResult.OK:
                        //Portforwarding Überprüfung! ÄNDERN
                        break;
                }
                MainWindow.FirstTimeDisableLocalOnly = false; //Wenn Portforwarding Prüfung erfolgreich!!! ÄNDERN
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            MainWindow.SettingsWindowIsOpened = false;
        }

        private void txbPath_PreviewMouseDown(object sender, MouseButtonEventArgs e) //txbPatch_Click
        {
            selectFolder = new FolderBrowserDialog();
            selectFolder.ShowNewFolderButton = true;
            selectFolder.SelectedPath = txbPath.Text;
            selectFolder.ShowDialog();
            if(selectFolder.SelectedPath != "") txbPath.Text = selectFolder.SelectedPath;
        }
    }
}
