using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ProjectP2P
{
    public class Settings
    {
        //------------Eigenschaften / Einstellungsvariabeln--------------------------
        public bool enableUDP { get; set; }
        public bool enableLocalOnly { get; set; }
        private byte _sendingProtocol;
        public bool listen { get; set; }
        private int _listenPort;
        public string path { get; set; }
        //Ereignisse - aus Settings-Objetk angetriggert
        public event EventHandler<EventArgs> UpdateMainFormEvent; //Für das antriggern der Objektmethode von der Klasse MainWindow UpdateMainForm()
        //Sonstiges - private
        private DirectoryInfo di;
        //-----------------Eigenschaftsmethoden (get,set)---------------------------
        public byte SendingProtocol
        {
            get
            {
                return _sendingProtocol;
            }
            set
            {
                if (value == 0 || value == 1) //0 = TCP, 1 = UDP
                {
                    _sendingProtocol = value;
                }
            }
        }
        public int ListenPort
        {
            get
            {
                return _listenPort;
            }
            set
            {
                if (value >= 49152 && value <= 65535)
                {
                    _listenPort = value;
                    if (UpdateMainFormEvent != null) UpdateMainFormEvent(this,new EventArgs()); //Dieses Objekt wird übergeben mit neuen Standart EvenArgs
                }
            }
        }
        //--------------Konstruktoren------------------------
        public Settings() //Standarteinstellungen
        {
            Debug.WriteLine("Erstelle neues Settings Objekt");
            enableLocalOnly = true;
            enableUDP = false;
            _sendingProtocol = 0;
            listen = true;
            _listenPort = 49155;
            path = Environment.CurrentDirectory + @"\Downloads";
            if (!Directory.Exists(Environment.CurrentDirectory + @"\Downloads"))
                di = Directory.CreateDirectory(path);
        }
        public Settings(Settings vorhanden) //Vorhande Einstellungen werden in aktuellem Objekt übernommen
        {
            Debug.WriteLine("Erstelle neues Settings Objekt aus vorhandenem Objekt");
            enableLocalOnly = vorhanden.enableLocalOnly;
            enableUDP = vorhanden.enableUDP;
            _sendingProtocol = vorhanden._sendingProtocol;
            listen = vorhanden.listen;
            _listenPort = vorhanden._listenPort;
            path = vorhanden.path;
        }
        //---------------ObjektMethoden-------------------
        public void SaveSettings()
        {
            Debug.WriteLine("Speichere aktuelles Objekt in settings.ini");
            string text = "#SettingsSave " + DateTime.Today.ToString("dd.MM.yy") + "\r\n#sendingProtocol=0 -> TCP sendingProtocol=1 -> UDP\r\nenableLocalOnly="+this.enableLocalOnly+"\r\nenableUDP=" + this.enableUDP.ToString() + "\r\nsendingProtocol=" +
                          this.SendingProtocol.ToString() + "\r\nlisten=" + this.listen.ToString() + "\r\nlistenPort=" +
                          this.ListenPort.ToString()+"\r\npath="+this.path;
            File.WriteAllText(Environment.CurrentDirectory + @"\settings.ini", text);
        }
        //-----------------statische Methoden-----------------------------
        public static Settings GetSavedSettings()
        {
            Settings newSettings;
            if (File.Exists(Environment.CurrentDirectory + @"\settings.ini"))
            {
                newSettings = new Settings();
                StreamReader txt = new StreamReader(Environment.CurrentDirectory + @"\settings.ini");
                string zeile;
                while ((zeile = txt.ReadLine()) != null)
                {
                    if (zeile[0] != '#')
                    {
                        string eigenschaft = zeile.Split('=')[0];
                        string eigenschaftInhalt = zeile.Split('=')[1];

                        switch (eigenschaft)
                        {
                            case "enableLocalOnly":
                                if (eigenschaftInhalt == "True") newSettings.enableLocalOnly = true;
                                if (eigenschaftInhalt == "False") newSettings.enableLocalOnly = false;
                                break;
                            case "enableUDP":
                                if (eigenschaftInhalt == "True") newSettings.enableUDP = true;
                                if (eigenschaftInhalt == "False") newSettings.enableUDP = false;
                                break;
                            case "sendingProtocol":
                                if (eigenschaftInhalt == "0") newSettings.SendingProtocol = 0;
                                if (eigenschaftInhalt == "1") newSettings.SendingProtocol = 1;
                                break;
                            case "listen":
                                if (eigenschaftInhalt == "True") newSettings.listen = true;
                                if (eigenschaftInhalt == "False") newSettings.listen = false;
                                break;
                            case "listenPort":
                                newSettings.ListenPort = Convert.ToInt32(eigenschaftInhalt);
                                break;
                            case "path":
                                newSettings.path = eigenschaftInhalt;
                                break;
                        }
                    }
                }
                Debug.WriteLine("settings.ini ausgelesen");
                txt.Close();
                return newSettings;
            }
            else
            {
                Debug.WriteLine("settings.ini exestiert nicht");
                newSettings = new Settings();
                newSettings.SaveSettings();
                return newSettings;
            }
        }
    }
}
