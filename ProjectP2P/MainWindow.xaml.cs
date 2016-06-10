using System;
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
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ThreadState = System.Threading.ThreadState;
using Timer = System.Timers.Timer;

namespace ProjectP2P
{
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
        internal static Timer timeout;
        //Zusätzliche Threads/Tasks statisch
        internal static Task DataRecieveWaiter;
        internal static Task<TcpClient> ListenerTask;
        internal static Task CheckAcknowledgeTask;
        internal static Task DataSenderTask;
        //internal static Task senderTask;
        //MainWindow-objekt Attribute
        internal TcpClient SenderTcpClient;
        internal TcpListenerAdapted listener;
        internal TcpClient data;
        internal NetworkStream tcpStream;
        internal UTF8Encoding encoding;
        //private Objekt Attribute
        private CancellationToken cancellationWaiting;
        private SettingsWindow settingsWindow;
        private ChatWindow chatWindow;
        private SyncDialogWindow syncDialogWindow;
        private string IpAdressInput;
        private bool RecievedAcknowledge;
        private bool RecievedNotAcknowledge;
        private bool IsTimeOut;
        private string[] SyncInfos;
        //--------------Konstruktor-------------------------
        public MainWindow()
        {
            Debug.WriteLine("Erstelle MainWindow Objekt");
            InitializeComponent();

            lblStatus.Content = "Prüft Internetverbindung..."; //Bis zum ersten aufruf von UpdateMainForm
            lblStatus.Foreground = Brushes.Purple;
            btnSync.IsEnabled = false;

            settings = new Settings(Settings.GetSavedSettings());
            profile = new Profile(Dispatcher);
            Profile.UpdateMainFormEvent += UpdateMainForm; //Die Methoden in den EventHandler hinzufügen
            settings.UpdateMainFormEvent += UpdateMainForm;
            encoding = new UTF8Encoding();

            synchronized = false; // <-- Anfänglich noch nicht Syncronisiert
            RecievedAcknowledge = false;
            RecievedAcknowledge = false;
            if (settings.enableLocalOnly)
                FirstTimeDisableLocalOnly = true;
            //Für das Dialogfenster in SettwingsWindow beim erstmaligen Deaktivieren
            else FirstTimeDisableLocalOnly = false;

            //UpdateMainForm(); --> Triggert nun der Profile.CheckInternetConnectionAndGetIPsTask  !!!
            NewStartOrStopOfListener();
        }

        //----------------EventMethoden--------------------------
        private void UpdateMainForm()
        {
            UpdateMainForm(this, new EventArgs());
        }

        private void UpdateMainForm(object sender, EventArgs eventArgs)
        {
            btnSync.IsEnabled = true; //Während der Internetconnectionprüfung soll dieser Button deaktiviert sein, erst hier wird dann aktiviert
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
                    cancellationWaiting.ThrowIfCancellationRequested();
                    //Bricht das Warten in WaitForData bzw. DataRecieveWaiter ab
                    Thread.Sleep(20); //Auf beendigung des DataRecieveWaiter warten [DataRecieve.Waiter.Wait() -> Macht den CancellationToken obsolete ... WHY :(]
                    listener.Stop();
                }
                try
                {
                    /*if(profile.localIPv6 != "") listener = new TcpListenerAdapted(IPAddress.Parse(profile.localIPv6), settings.ListenPort);
                    else listener = new TcpListenerAdapted(IPAddress.Parse(profile.localIPv4), settings.ListenPort);*/
                    //<<<<----- Abhöhren auf IPv6 funktioniert noch nicht. Bisher nur auf v4 ------>>>>>>>
                    if (profile.localIPv4 == null)
                        Thread.Sleep(1000);
                    //Eine Sekunde auf Profile.CheckInternetConnectionAndGetIPsTask warten damit die IPs gegeben sind
                    listener = new TcpListenerAdapted(IPAddress.Parse(profile.localIPv4), settings.ListenPort);
                    listener.Start();
                }
                catch (FormatException)
                {
                    MessageBox.Show(
                            "Keine Internetverbidnung\n\nBitte stellen Sie eine gültige Verbindung her und versuchen Sie es erneut\nAtomatisches Abhöhren deaktiviert",
                            "Keine Internetverbindung", MessageBoxButton.OK, MessageBoxImage.Error);
                    settings.listen = false;
                    UpdateMainForm();
                    NewStartOrStopOfListener();
                    settings.SaveSettings();
                }
                catch (Exception)
                {
                    MessageBox.Show(
                        "Der angegebene Port ist blockiert. Automatischen Abhöhren deaktiviert.\n\nBitte geben Sie einen offenen Port an und porbieren es erneut.",
                        "Port blockiert", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void btnText_Click()
        {
            btnText_Click(this, new RoutedEventArgs());
        }
        private void btnText_Click(object sender, RoutedEventArgs e)
        {
            if (!ChatWindowIsOpened)
            {
                chatWindow = new ChatWindow();
                ChatWindowIsOpened = true;
                chatWindow.SendText += SendChatText;
                chatWindow.Show();
            }
        }

        private void btnSync_Click(object sender, RoutedEventArgs e)
        {
            if (!synchronized)
            {
                if(SettingsWindowIsOpened) settingsWindow.Close();
                btnSettings.IsEnabled = false;
                btnSync.IsEnabled = false;
                SendSyncRequest();
            }
            else
            {
                partner = null;
                synchronized = false;
                if (ChatWindowIsOpened) chatWindow.Close();
                if (SettingsWindowIsOpened) settingsWindow.Close();
                try
                {
                    StartDataSenderTask("27|04", SyncInfos[1], false); //27|4 -> ESC
                }
                catch (Exception)
                {
                    MessageBox.Show("Der Partner reagiert nichtmehr.", "Fehler!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (synchronized) SendData("27|04", partner.IPv4, false); //EINZIGE STELLE an der Mainthread das senden Übernimmt
        }

        private void SyncNo(object sender, EventArgs e)
        {
            StartDataSenderTask("21|04", SyncInfos[1], false); //Nochma wegen IPv6 nachschlagen SyncInfos[1] -> IPV4
            NewStartOrStopOfListener();
        }

        private void SyncYes(object sender, EventArgs e)
        {
            syncDialogWindow.Close();
            partner = new PartnerProfile(SyncInfos[0], SyncInfos[1], SyncInfos[2], SyncInfos[3]);
            if (partner.IsLocalIPv4 || partner.IsLocalIPv6)
            {
                StartDataSenderTask("06|" + profile.localIPv6 + "|" + profile.localIPv4 + "|" + profile.id + "|04", partner.IPv4, false); //06 = ACK -> acknowledge 
            }
            else
            {
                StartDataSenderTask("06|" + profile.externIPv6 + "|" + profile.externIPv4 + "|" + profile.id + "|04", partner.IPv4, false); //06 = ACK -> acknowledge 
            }
            synchronized = true;
            UpdateMainForm();
        }

        private void SendChatText(object sender, EventArgs<string> e)
        {
            StartDataSenderTask("02|" + e.Data + "|0304", partner.IPv4, false);
        }
        //------------------ObjektMethoden--------------------
        private void DataRecieve() //RUFT NICHT DER MAINTHREAD AUF, SONDERN DER "DataRecieveWaiter"
        {
            StreamReader reader = new StreamReader(data.GetStream());
            string TcpClientIp = ((IPEndPoint)data.Client.RemoteEndPoint).Address.ToString();
            string text = reader.ReadLine();
            string[] informationArray;
            try
            {
                informationArray = text.Split('|'); //infromationArray[0] -> Präambel (siehe oben) | [1] -> IPv6 | [2] -> IPv4 | [3] -> ID | [4] -> EOT = 04 (End of Transmission)
            }
            catch
            {
                return;
            }

            if (informationArray[0] == "06" && informationArray[4] == "04") // ACK -> acknowledge / Bestätigung
            {
                partner = new PartnerProfile(informationArray[1], informationArray[2], informationArray[3], TcpClientIp); //ACK sendet weiterhin auch die IPv4,IPv6 & Id des Gegenübers
                synchronized = true;
                RecievedAcknowledge = true;
                //Für die zeitlich abhängige Prüfung, ob das Acknowledge bereits angekommen ist.
                Dispatcher.Invoke(new Action(UpdateMainForm));
                informationArray = null;
                text = null;
                data = null;
                return;
            }

            if (informationArray[0] == "21" && informationArray[1] == "04") // NAK -> Not Acknowledge
            {
                RecievedNotAcknowledge = true;
                MessageBox.Show("Die Synchronisation wurde abgelehnt.", "Abgelehnt!", MessageBoxButton.OK, MessageBoxImage.Hand);
                informationArray = null;
                text = null;
                data = null;
                return;
            }

            if (informationArray[0] == "222201" && informationArray[4] == "04") //22 = <SYN> 01 = <SOH> | 22 22 01 = <SYN><SYN><SOH> (222201)  Synchronisationsanfrage
            {
                timeout = new Timer(20000);
                timeout.AutoReset = false;
                timeout.Elapsed += delegate
                {
                    timeout.Stop();
                    Dispatcher.InvokeAsync(syncDialogWindow.Close);
                    NewStartOrStopOfListener();
                    MessageBox.Show("Zeitüberschreitung der Anfrage", "Zeitüberschreitung", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                };
                Dispatcher.Invoke(() => syncDialogWindow = new SyncDialogWindow(informationArray));
                syncDialogWindow.Yes += SyncYes;
                syncDialogWindow.No += SyncNo;
                Dispatcher.Invoke(syncDialogWindow.Show);
                SyncInfos = new string[] { informationArray[1], informationArray[2], informationArray[3], TcpClientIp };
                informationArray = null;
                timeout.Stop();
                timeout.Enabled = false;
                //timeout = null;
                text = null;
                data = null;
                return;
            }


            if (informationArray[0] == "27" && informationArray[1] == "04" && synchronized == true && partner.TcpClientIp == TcpClientIp) //ESC --> Escape / Abbruch der Synchronisation
            {
                synchronized = false;
                if (ChatWindowIsOpened) Dispatcher.Invoke(chatWindow.Close);
                Dispatcher.Invoke(UpdateMainForm);
                Dispatcher.Invoke(NewStartOrStopOfListener);
                MessageBox.Show("Dein Partner hat die aktuelle Sitzung beendet.", "Sitzung beendet",
                MessageBoxButton.OK, MessageBoxImage.Information);
                informationArray = null;
                text = null;
                data = null;
                return;
            }

            if (informationArray[0] == "02" && informationArray[2] == "0304" && synchronized == true && partner.TcpClientIp == TcpClientIp) //02 -> STX | 03 -> ETX / Text
            {
                if (!ChatWindowIsOpened)
                {
                    Dispatcher.Invoke(btnText_Click); //Ruft das Klick-Event btnText auf (öffnet Chat Fenster)
                }
                Dispatcher.Invoke(() => chatWindow.RecieveText(informationArray[1]));
                Dispatcher.Invoke(NewStartOrStopOfListener); // Auf neue Informationen horchen
                Dispatcher.Invoke(UpdateMainForm); //Damit die Statusanzeige weiterhin die aktuelle Lage anzeigt
            }
        }

        private void SendSyncRequest()
        {
            byte IpAdressCheck = CheckIpAdress(txbVerbinden.Text);
            //if(txbVerbinden.Text == profile.localIPv4)
            if (settings.enableLocalOnly && IpAdressCheck > 1) //Check auf lokale IPs
            {
                MessageBox.Show("Sie haben nur lokale Verbindungen aktiviert.\nBitte geben Sie eine lokale IP-Adresse an,\noder überarbeiten Sie Ihre Einstellungen.", "Keine lokale IP", MessageBoxButton.OK, MessageBoxImage.Warning);
                UpdateMainForm();
                return;
            }
            else if (!settings.enableLocalOnly && IpAdressCheck < 2)
            {
                MessageBox.Show("Sie haben nur externe Verbindungen aktiviert.\nBitte geben Sie eine externe IP-Adresse an,\noder überarbeiten Sie Ihre Einstellungen.", "Keine externe IP", MessageBoxButton.OK, MessageBoxImage.Warning);
                UpdateMainForm();
                return;
            }
            if (IpAdressCheck >= 0 && IpAdressCheck <= 3) //0-3 gültige IPs, bei 255 -> Fehler
            {
                if (settings.enableLocalOnly)
                {
                    StartDataSenderTask("222201|" + profile.localIPv6 + "|" + profile.localIPv4 + "|" + profile.id +
                                          "|04", txbVerbinden.Text, true);
                }
                else
                {
                    StartDataSenderTask("222201|" + profile.externIPv6 + "|" + profile.externIPv4 + "|" +
                                          profile.id + "|04", txbVerbinden.Text, true);
                }
            }
            else
            {
                if (settings.enableLocalOnly)
                {
                    MessageBox.Show("Die eingegebene lokale IP ist ungültig.\nBitte überprüfen Sie die Eingabe.",
                        "Eingabefehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Die eingegebene IP ist ungültig.\nBitte überprüfen Sie die Eingabe.",
                        "Eingabefehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void StartDataSenderTask(string text, string ip, bool SyncRequest)
        {
            Debug.WriteLine("Wartet auf beendigung des DataSenderTask");
            lblStatus.Content = "Warte auf beendigung des Sendens...";
            lblStatus.Foreground = Brushes.Red;
            if (DataSenderTask != null) DataSenderTask.Wait(); //<-- Auf beendigung der aktuellen Aufgabe warten

            DataSenderTask = new Task(() => SendData(text, ip, SyncRequest));

            lblStatus.Content = "Verbindungsaufbau...";
            lblStatus.Foreground = Brushes.Red;

            DataSenderTask.Start();
        }
        private void SendData(string text, string ip, bool SyncRequest) //string ip --> Zum unterscheiden der überladenen Methoden || WIRD NICHT VOM MAIN THREAD AUSGEFÜHRT
        {
            SenderTcpClient = new TcpClient(AddressFamily.InterNetworkV6);
            SenderTcpClient.Client.DualMode = true; // IPv6 & IPv4 erlauben --> Dualmode seid .NET 4.5
            tcpStream = null;
            Byte[] bytePackage;
            try
            {
                SenderTcpClient.Connect(IPAddress.Parse(ip), settings.ListenPort);
                tcpStream = SenderTcpClient.GetStream();
            }
            catch (SocketException)
            {
                MessageBox.Show(
                    "Ein Verbindunsgversuch ist fehlgeschlagen,\nda die Gegenstelle nach einer bestimmten Zeitspanne\nnicht reagiert hat.\n\nÜberprüfen Sie die IP und Ihre Einstellungen.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Dispatcher.Invoke(UpdateMainForm);
                return;
            }
            catch (Exception e)
            {
                MessageBox.Show(
                        "Kritischer Fehler:\n" + e,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Dispatcher.Invoke(UpdateMainForm);
                Dispatcher.Invoke(NewStartOrStopOfListener);
                return;
            }
            if (tcpStream != null)
            {
                bytePackage = encoding.GetBytes(text);
                tcpStream.Write(bytePackage, 0, bytePackage.Length);
                SenderTcpClient.Close();

                NewStartOrStopOfListener();
                //Falls localOnly = true, muss der Listener ab diesem Zeitpunkt trd. gestartet werden um das ACK abzuhöhren
                if (SyncRequest)
                {
                    Dispatcher.Invoke(delegate //anonyme Methode für aktuallisierung des Statuslabels
                    {
                        lblStatus.Content = "Warte auf Antwort...";
                        lblStatus.Foreground = Brushes.Red;
                    });

                    CheckAcknowledgeTask = new Task(() => CheckIfAcknowledgeRecieved(ip));
                    CheckAcknowledgeTask.Start();
                }
                else
                {
                    Dispatcher.Invoke(UpdateMainForm);
                }
            }
        }

        private void SendData(string path, IPAddress ip) //IPAddress --> Zum unterscheiden der überladenen Methoden
        {
            //tcpFileStream = new FileStream(dataText,);
        }

        private void WaitForData()
        {
            Debug.WriteLine("Warte auf Reaktion des ListenerTask");
            try
            {

                ListenerTask.Wait(cancellationWaiting);
                Debug.WriteLine("Daten ehalten");
                data = ListenerTask.Result;
                if (data != null) DataRecieve();
                else NewStartOrStopOfListener();
            }
            catch (AggregateException)
            {
                Debug.WriteLine("Das Warten wurde abgebrochen");
            }
        }

        private void CheckIfAcknowledgeRecieved(string IpAdress)
        //Für die Ausgabe (Threadübergreifend, daher als Parameter übergeben)
        {
            timeout = new Timer(19999);
            RecievedAcknowledge = false;
            RecievedNotAcknowledge = false;
            IsTimeOut = false;
            timeout.Elapsed += delegate // Sogenannte anonyme Methode -> Hier sinnvoll da nur einmal verwendet
            {
                IsTimeOut = true;
                timeout.Stop();
            };
            timeout.Start();
            while (!IsTimeOut)
            {
                if (RecievedAcknowledge)
                {
                    synchronized = true;
                    RecievedAcknowledge = true;
                    timeout.Stop();
                    Dispatcher.Invoke(UpdateMainForm);
                    Dispatcher.Invoke(NewStartOrStopOfListener); //Neustart des Listeneres
                    return;
                }
                if (RecievedNotAcknowledge)
                {
                    RecievedNotAcknowledge = true;
                    timeout.Stop();
                    Dispatcher.Invoke(UpdateMainForm);
                    Dispatcher.Invoke(NewStartOrStopOfListener);
                    return;
                }
                Thread.Sleep(100);
            }
            if (!RecievedAcknowledge)
            {
                MessageBox.Show(
                    "Die Synchronisation zu " + IpAdress + " ist gescheitert.\nZeitüberschreitung der Verbindung.",
                    "Zeitüberschreitung", MessageBoxButton.OK, MessageBoxImage.Error);
                timeout.Stop();
                Dispatcher.Invoke(UpdateMainForm);
                return;
            }
        }

        internal static byte CheckIpAdress(string ipString) //Byte Rückgabe ==>  0=LocalIPv4 | 1=LocalIPv6 | 2=ExternIPv4 | 3=ExternIPv6 | 255=Fehler
        {
            IPAddress address;
            if (IPAddress.TryParse(ipString, out address))
            {
                switch (address.AddressFamily)
                {
                    case AddressFamily.InterNetwork: //IPv4
                        string[] ipv4Blocks = ipString.Split('.');
                        byte block0 = Convert.ToByte(ipv4Blocks[0]);
                        byte block1 = Convert.ToByte(ipv4Blocks[1]);

                        if (block0 == 10 || (block0 == 172 && (block1 >= 16 && block1 <= 31)) ||
                            (block0 == 192 && block1 == 168) || block0 == 127) //LocalPrüfung
                        {
                            return 0; //LocalIPv4
                        }
                        else return 2; //ExternIPv4
                        break;
                    case AddressFamily.InterNetworkV6: //IPv6
                        if (address.IsIPv6LinkLocal) return 1; //LocalIPv6
                        else return 3; //ExternIPv6
                        break;
                    default:
                        return 255; //Fehler
                        break;
                }
            }
            return 255; //Fehler
        }
    }
}


