﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThreadState = System.Threading.ThreadState;
using Timer = System.Timers.Timer;

namespace ProjectP2P
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //----------Eigenschaften---------------------
        //Objekte der anderen Klassen statisch in MainWindow gehalten 
        internal static Settings settings;
        internal static Profile profile;
        internal static PartnerProfile partner;
        //Hilfsvariabeln statisch
        internal static bool SettingsWindowIsOpened;
        internal static bool ChatWindowIsOpened;
        internal static bool FirstTimeDisableLocalOnly;
        internal static bool synchronized;
        //Zusätzliche Threads/Tasks statisch
        internal static Task DataRecieveWaiter;
        internal static Task<TcpClient> ListenerTask;
        internal static Task CheckAcknowledgeTask;
        //internal static Task senderTask;
        //MainWindow-objekt Attribute
        internal TcpClient sender;
        internal TcpListenerAdapted listener;
        internal TcpClient data;
        internal UTF8Encoding encoding;
        //private Objekt Attribute
        private CancellationToken cancellationWaiting;
        private SettingsWindow settingsWindow;
        private ChatWindow chatWindow;
        private string IpAdressInput;
        private bool RecievedAcknowledge;
        private bool IsTimeOut;
        //--------------Konstruktor-------------------------
        public MainWindow()
        {
            Debug.WriteLine("Erstelle MainWindow Objekt");
            InitializeComponent();
            settings = new Settings(Settings.GetSavedSettings());
            profile = new Profile();
            settings.UpdateMainFormEvent += UpdateMainForm; //Die Methode in den EventHandler hinzufügen
            encoding = new UTF8Encoding();

            synchronized = false; // <-- Anfänglich noch nicht Syncronisiert
            RecievedAcknowledge = false;
            if (settings.enableLocalOnly) FirstTimeDisableLocalOnly = true; //Für das Dialogfenster in SettwingsWindow beim erstmaligen Deaktivieren
            else FirstTimeDisableLocalOnly = false;

            UpdateMainForm();
            NewStartOrStopOfListener();
        }
        //----------------EventMethoden--------------------------
        private void UpdateMainForm()
        {
            UpdateMainForm(this, new EventArgs());
        }
        private void UpdateMainForm(object sender, EventArgs eventArgs)
        {
            if (!synchronized)
            {
                //Sichtbarkeit des Anfangsfensters
                InfoGrid.Visibility = Visibility.Visible;
                VerbindenGrid.Visibility = Visibility.Visible;
                lblInfoText.Visibility = Visibility.Visible;
                lblVerbindenText.Visibility = Visibility.Visible;
                BorderInfo.Visibility = Visibility.Visible;
                BorderVerbinden.Visibility = Visibility.Visible;
                btnText.Visibility = Visibility.Hidden;
                btnFile.Visibility = Visibility.Hidden;
                btnSettings.IsEnabled = true;
                btnSync.Content = "Synchronisieren!";

                //Aktualisieren:
                txbPort.Text = Convert.ToString(settings.ListenPort);
                txbLocalIpv4.Text = profile.localIPv4;
                txbLocalIpv6.Text = profile.localIPv6;
                txbExternIpv4.Text = profile.externIPv4;
                txbExternIpv6.Text = profile.externIPv6;
                txbId.Text = Convert.ToString(profile.id);
                if (Profile.InternetConnection)
                {
                    if (settings.enableLocalOnly)
                    {
                        lblStatus.Foreground = Brushes.DarkOrange;
                        if (settings.listen)
                        {
                            lblStatus.Content = "Bereit - nur Lokal";
                        }
                        else
                        {
                            lblStatus.Content = "Abhöhren deaktiviert - nur Lokal";
                        }
                    }
                    else
                    {
                        lblStatus.Foreground = Brushes.Green;
                        if (settings.listen)
                        {
                            lblStatus.Content = "Bereit";
                        }
                        else
                        {
                            lblStatus.Content = "Abhören deaktiviert";
                        }
                    }
                }
                else
                {
                    lblStatus.Content = "Keine Internetverbindung";
                    lblStatus.Foreground = Brushes.Red;
                }
            }
            else
            {
                //Sichtbarkeit der Datenübertragungen
                InfoGrid.Visibility = Visibility.Hidden;
                VerbindenGrid.Visibility = Visibility.Hidden;
                lblInfoText.Visibility = Visibility.Hidden;
                lblVerbindenText.Visibility = Visibility.Hidden;
                BorderInfo.Visibility = Visibility.Hidden;
                BorderVerbinden.Visibility = Visibility.Hidden;
                btnText.Visibility = Visibility.Visible;
                btnFile.Visibility = Visibility.Visible;
                btnSettings.IsEnabled = false;
                if (SettingsWindowIsOpened) settingsWindow.Close();
                btnSync.Content = "Synchronisation beenden";

                lblStatus.Foreground = Brushes.Blue;
                lblStatus.Content = "Synchronisiert";
            }
        }

        public void NewStartOrStopOfListener()
        {
            NewStartOrStopOfListener(this, new EventArgs());
        }
        public void NewStartOrStopOfListener(object sender, EventArgs eventArgs)
        {
            if (settings.listen)
            {
                if (listener != null)
                {
                    cancellationWaiting.ThrowIfCancellationRequested(); //Bricht das Warten in WaitForData bzw. DataRecieveWaiter ab
                    Thread.Sleep(20); //Auf beendigung des DataRecieveWaiter warten [DataRecieve.Waiter.Wait() -> Macht den CancellationToken obsolete ... WHY :(]
                    listener.Stop();
                }
                try
                {
                    listener = new TcpListenerAdapted(IPAddress.Parse("127.0.0.1"), settings.ListenPort); //Loopback zum Testen
                    listener.Start();
                }
                catch
                {
                    MessageBox.Show("Der angegebene Port ist blockiert. Automatischen abhöhren deaktiviert.\n\nBitte geben Sie einen offenen Port an und porbieren es erneut.", "Port blockiert", MessageBoxButton.OK, MessageBoxImage.Error);
                    settings.listen = false;
                    UpdateMainForm();
                    NewStartOrStopOfListener();
                    settings.SaveSettings();
                }
                if (settings.listen) //Doppelte Überprüfung, falls Error wichtig
                {
                    ListenerTask = listener.AcceptTcpClientAsync();
                    DataRecieveWaiter = new Task(WaitForData); //Neuinitiallisierung notwendig
                    DataRecieveWaiter.Start();
                    Debug.WriteLine("Listening + Waiting wurde neu initiallisiert und neu gestartet");
                }

            }
            else
            {
                if (listener != null)
                {
                    cancellationWaiting.ThrowIfCancellationRequested();
                    listener.Stop();
                    Debug.WriteLine("Listening + Waiting wurde gestoppt");
                }
            }
        }
        private void btnText_Click(object sender, RoutedEventArgs e)
        {
            if (!ChatWindowIsOpened)
            {
                chatWindow = new ChatWindow();
                ChatWindowIsOpened = true;
                chatWindow.Show();
            }
        }
        private void btnSync_Click(object sender, RoutedEventArgs e)
        {
            if (!synchronized)
            {
                SendSyncRequest();
            }
            else
            {
                partner = null;
                synchronized = false;
                if (ChatWindowIsOpened) chatWindow.Close();
                UpdateMainForm();
                NewStartOrStopOfListener();
            }
        }
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!SettingsWindowIsOpened)
            {
                settingsWindow = new SettingsWindow();
                settingsWindow.StartStopListener += NewStartOrStopOfListener;
                SettingsWindowIsOpened = true;
                settingsWindow.Show();
            }
        }
        //------------------ObjektMethoden--------------------
        private void DataRecieve()
        {
            StreamReader reader = new StreamReader(data.GetStream());
            string text = reader.ReadLine();
            string[] informationArray;

            if (text.Substring(0, 6) == "222201") //22 = <SYN> 01 = <SOH> | 22 22 01 = <SYN><SYN><SOH> (222201)  Synchronisationsanfrage
            {
                informationArray = text.Split('|'); //infromationArray[0] -> Präambel (siehe oben) | [1] -> IPv6 | [2] -> IPv4 | [3] -> ID | [4] -> EOT = 04 (End of Transmission)
                if (informationArray[4] == "04") //Entspricht den Rahmenbedingungen
                {
                    MessageBoxResult result = MessageBox.Show("Der User mit der ID: " + informationArray[3] + " möchte sich mit Ihnen synchronisieren.\nIPv6: " + informationArray[1] + "\nIPv4: " + informationArray[2] + "\n\nMöchten Sie die Anfrage annehmen?", "Synchronisationsanfrage", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    switch (result)
                    {
                        case MessageBoxResult.No:
                            text = null;
                            informationArray = null;
                            Dispatcher.Invoke(new Action(NewStartOrStopOfListener));
                            break;
                        case MessageBoxResult.Yes:
                            partner = new PartnerProfile(informationArray[1], informationArray[2], informationArray[3]);
                            if (partner.IsLocalIPv4 || partner.IsLocalIPv6)
                            {
                                SendData("06|" + profile.localIPv6 + "|" + profile.localIPv4 + "|" + profile.id + "|04"); //06 = ACK -> acknowledge
                            }
                            else
                            {
                                SendData("06|" + profile.externIPv6 + "|" + profile.externIPv4 + "|" + profile.id + "|04");
                            }
                            synchronized = true;
                            Dispatcher.Invoke(new Action(UpdateMainForm)); //DISPATCHER & INVOKE lernen!
                            break;
                    }
                }
            }
            if (text.Substring(0, 2) == "06") // ACK -> acknowledge / Bestätigung
            {
                informationArray = text.Split('|');
                if (informationArray[4] == "04")
                {
                    partner = new PartnerProfile(informationArray[1], informationArray[2], informationArray[3]);
                    synchronized = true;
                    RecievedAcknowledge = true; //Für die zeitlich abhängige Prüfung, ob das Acknowledge bereits angekommen ist.
                    Dispatcher.Invoke(new Action(UpdateMainForm));
                }
            }

        }

        private void SendSyncRequest()
        {
            if (!CheckIpAdress(txbVerbinden.Text)) //failed = false -> Keine Probleme mit der eingegeben IP
            {
                sender = new TcpClient();
                Stream tcpStream = null;
                string IpAdressString = txbVerbinden.Text; //Weil er unten bei der Übergabe meckert -> Aufgrund der späteren übernahme des Objekts eines anderen Threads
                try
                {
                    sender.Connect(IPAddress.Parse(txbVerbinden.Text), settings.ListenPort);
                    tcpStream = sender.GetStream();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unbekannter Fehler:\n" + e, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                if (tcpStream != null)
                {
                    Byte[] bytePackage;
                    if (settings.enableLocalOnly)
                    {
                        bytePackage = encoding.GetBytes("222201|" + profile.localIPv6 + "|" + profile.localIPv4 + "|" + profile.id + "|04");
                    }
                    else
                    {
                        bytePackage = encoding.GetBytes("222201|" + profile.externIPv6 + "|" + profile.externIPv4 + "|" + profile.id + "|04");
                    }
                    tcpStream.Write(bytePackage, 0, bytePackage.Length);
                    sender.Close();
                    lblStatus.Content = "Warte auf Antwort...";
                    lblStatus.Foreground = Brushes.Red;
                    NewStartOrStopOfListener(); //Falls localOnly = true, muss der Listener ab diesem Zeitpunkt trd. gestartet werden um das ACK abzuhöhren
                    CheckAcknowledgeTask = new Task(() => CheckIfAcknowledgeRecieved(IpAdressString));
                    CheckAcknowledgeTask.Start();
                }
            }
            else
            {
                if (settings.enableLocalOnly)
                {
                    MessageBox.Show("Die eingegebene lokale IP ist ungültig.\nBitte überprüfen Sie die Eingabe.", "Eingabefehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Die eingegebene IP ist ungültig.\nBitte überprüfen Sie die Eingabe.", "Eingabefehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        private void SendData(object data)
        {

        }

        private void WaitForData()
        {
            Debug.WriteLine("Warte auf Reaktion des ListenerTask");
            try
            {
                ListenerTask.Wait(cancellationWaiting);
                Debug.WriteLine("Daten ehalten");
                data = ListenerTask.Result;
                DataRecieve();
            }
            catch (AggregateException)
            {
                Debug.WriteLine("Das Warten wurde abgebrochen");
            }
        }

        private void CheckIfAcknowledgeRecieved(string IpAdress) //Für die Ausgabe (Threadübergreifend, daher als Parameter übergeben)
        {
            Timer timeout = new Timer(5000);
            IsTimeOut = false;
            timeout.Elapsed += delegate // Sogenannte anonyme Methode -> Hier sinnvoll da nur einmal verwendet
            {
                IsTimeOut = true;
            };
            timeout.Start();
            while (!IsTimeOut)
            {
                if (RecievedAcknowledge)
                {
                    synchronized = true;
                    RecievedAcknowledge = false;
                    timeout.Stop();
                    Dispatcher.Invoke(UpdateMainForm);
                    Dispatcher.Invoke(NewStartOrStopOfListener); //Neustart des Listeneres
                }
                Thread.Sleep(100);
            }
            if (!RecievedAcknowledge)
            {
                MessageBox.Show("Die Synchronisation zu " + IpAdress + " ist gescheitert.\nZeitüberschreitung der Verbindung.", "Zeitüberschreitung", MessageBoxButton.OK, MessageBoxImage.Error);
                timeout.Stop();
                Dispatcher.Invoke(UpdateMainForm);
            }
        }

        internal static bool CheckIpAdress(string ipString)
        {
            bool failed = true;
            if (ipString.Length >= 7 && ipString.Length <= 15) //IPv4 Check
            {
                string[] blocks = new string[4];
                blocks = ipString.Split('.');
                byte[] blocksByte = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    if (blocks[i] != null || blocks[i] != "")
                    {
                        try // Falsche Eingaben oder größere Eingaben, als 255 pro Block abfangen
                        {
                            blocksByte[i] = Convert.ToByte(blocks[i]);
                            failed = false;
                        }
                        catch
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }

                if (blocksByte[0] != 0 || blocksByte[1] != 0 || blocksByte[2] != 0 || blocksByte[3] != 0) // 0.0.0.0 ausschließen
                {
                    if (blocksByte[0] != 255 || blocksByte[1] != 255 || blocksByte[2] != 255 || blocksByte[3] != 255) // 255.255.255.255 auschließen
                    {
                        if (blocksByte[0] != 0/*127*/) //Loopback Adressbereich ausschließen
                        {
                            if (settings.enableLocalOnly) //Nur lokaler Adressbereich
                            {
                                if (blocksByte[0] == 10) //lokaler Adressbereich: 10.0.0.0 - 10.255.255.255
                                {
                                    failed = false;
                                }
                                else if (blocksByte[0] == 100) //lokaler Adressbereich 100.64.0.0 - 100.127.255.255
                                {
                                    if (blocksByte[1] >= 64 && blocksByte[1] <= 127)
                                    {
                                        failed = false;
                                    }
                                    else
                                    {
                                        return true;
                                    }
                                }
                                else if (blocksByte[0] == 172) //lokaler Adressbereich 172.16.0.0 - 172.31.255.255
                                {
                                    if (blocksByte[1] >= 16 && blocksByte[1] <= 31)
                                    {
                                        failed = false;
                                    }
                                    else
                                    {
                                        return true;
                                    }
                                }
                                else if (blocksByte[0] == 192) //lokaler Adressbereich 192.168.0.0 - 192.168.255.255
                                {
                                    if (blocksByte[1] == 168)
                                    {
                                        failed = false;
                                    }
                                    else
                                    {
                                        return true;
                                    }
                                }
                                else if (blocksByte[0] == 127) //NUR ZUM TESTEN, SPÄTER WIEDER ENTFERNEN
                                {
                                    failed = false;
                                }
                                else //Kein lokaler Adressbereich
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                failed = false;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }

            }
            else
            {
                return true;
            }
            //-------------------------------------------------------
            if (!failed)
            {
                return false;
            }
            else return true;
        }



    }
}