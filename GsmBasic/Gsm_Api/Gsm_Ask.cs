using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MelBox
{
    public partial class Gsm
    {

        #region Properties
        /// <summary>
        /// Liste der zu sendenden Nachrichten. 
        /// Wird genutzt, um Zeitpunkt für Senden und Modem-Antwort zeitlich zu koordinieren
        /// </summary>
        internal static List<Tuple<ulong, string>> SendQueue { get; set; } = new List<Tuple<ulong, string>>();

        //private static bool SendBlock = false;

        //private static bool ReadBlock = false;

        #endregion


        #region Events aus Interpretations-Methoden

        /// <summary>
        /// Event SMS empfangen
        /// </summary>
        public event EventHandler<ShortMessageArgs> RaiseSmsRecievedEvent;

        /// <summary>
        /// Trigger für das Event SMS empfangen
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRaiseSmsRecievedEvent(ShortMessageArgs e)
        {
            RaiseSmsRecievedEvent?.Invoke(this, e);
        }

        /// <summary>
        /// Event 'Signalqualität'
        /// </summary>
        public event EventHandler<GsmEventArgs> RaiseGsmQualityEvent;

        /// <summary>
        /// Trigger für das Event 'Signalqualität Mobilfunknetz ermittelt'
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRaiseGsmQualityEvent(GsmEventArgs e)
        {
            RaiseGsmQualityEvent?.Invoke(this, e);
        }

        #endregion


        public void SetupGsm()
        {
            RaiseGsmRecEvent += InterpretGsmRecEvent;

            //Textmode
            SendATCommand("AT+CMGF=1");

            //SendATCommand("AT+CPMS=\"SM\""); //ME, SM, MT
            //SendATCommand("AT+CPMS=\"MT\",\"MT\",\"MT\"");
            SendATCommand("AT+CPMS=\"SM\",\"SM\",\"SM\"");

            //Erzwinge, dass bei Fehlerhaftem senden "+CMS ERROR: <err>" ausgegeben wird
            SendATCommand("AT^SM20=0,0");

            //SIM-Karte im Mobilfunknetz registriert?
            SendATCommand("AT+CREG?");

            //Signalqualität
            SendATCommand("AT+CSQ");

            //Sendeempfangsbestätigungen abonieren
            //Quelle: https://www.codeproject.com/questions/271002/delivery-reports-in-at-commands
            //Quelle: https://www.smssolutions.net/tutorials/gsm/sendsmsat/
            SendATCommand("AT+CSMP=49,1,0,0");

            SendATCommand("AT+CNMI=2,1,2,2,1");

            //Startet Timer zum wiederholten Abrufen von Nachrichten
            SetCyclicTimer();
        }

        /// <summary>
        /// Wird intervallweise getriggert, durchläuft einen kompletten Empfangs-/Sende-Zyklus
        /// </summary>
        private void Loop()
        {
            //1) anstehende SMS auf SIM speichern 'AT+CMGW'
            AddSmsToStorage();

            //2) alle SMS lesen 'AT+CMGL="ALL"'
            SmsRead();

            //3)        bearbeite Antwort aus GSM-Modem als Ereignis
            //3.1.1)    "REC UNREAD" Neu angekommene SMS in DB speichern
            //3.1.2)    "REC READ" Nachrichten aus SIM löschen. 
            //3.2.1)    Sendebestätigungen verarbeiten
            //3.4)  Gelesene Sendebestätigungen aus SIM löschen    
            //3.5)  Zeitüberschreitung Sendebestätigung melden 
            //3.6)  "STO UNSENT" Zu sendende SMS aus Speicher senden  => "AT+CMSS"
            //3.7)  "STO SENT" geendende SMS aus Speicher löschen

            //4) optional: erfrage Mobilfunknetzqualität
            SendATCommand("AT+CSQ");

        }

        #region SMS lesen


        private void SmsRead(string filter = "ALL")
        {
            SendATCommand("AT+CMGL=\"" + filter + "\"");
        }

        #endregion

        #region SMS verschicken

        /// <summary>
        /// Packe 'SMS zum Senden' in die Liste zur Abarbeitung
        /// </summary>
        /// <param name="phone">Format mit Ländervorwahl: 49123456789</param>
        /// <param name="content">SMS-Text; Zeilenumbrüche werden entfernt.</param>
        public static void SmsSend(ulong phone, string content)
        {
            Tuple<ulong, string> t = new Tuple<ulong, string>(phone, content);
            if (!SendQueue.Contains(t))
                SendQueue.Add(t);
        }


        /// <summary>
        /// Schreibt alle in SendQueue anstehenden SMS in den GSM-Speicher zum Versenden
        /// </summary>
        /// <param name="phone">Format mit Ländervorwahl: 49123456789</param>
        /// <param name="content">SMS-Text; Zeilenumbrüche werden entfernt.</param>
        /// <returns>ID der gesendeten SMS</returns>
        private void AddSmsToStorage()
        {
            if (SendQueue.Count < 1) return;

            foreach(Tuple<ulong,string> sms in SendQueue)
            {
                ulong phone = sms.Item1;
                string content = sms.Item2;
                
                OnRaiseGsmSystemEvent(new GsmEventArgs(11051543, "Sende an: " + phone + " - " + content));

                const string ctrlz = "\u001a";
                content = content.Replace("\r\n", " ");
                if (content.Length > 160) content = content.Substring(0, 160);
                
                SendATCommand("AT+CMGW=\"+" + phone + "\"\r");
                //Thread.Sleep(200); //Angstpause
                SendATCommand(content + ctrlz);
                Thread.Sleep(500); //Angstpause

                SendQueue.Remove(sms);
            }
        }

        #endregion

        #region SMS löschen
        /// <summary>
        /// Löscht eine SMS aus dem Speicher
        /// </summary>
        /// <param name="smsId">Id der SMS im GSM-Speicher</param>
        private void SmsDelete(int smsId)
        {
            //DUMMY nur melden:
            OnRaiseGsmSystemEvent(new GsmEventArgs(11111726, "Die Nachricht mit der Id " + smsId + " würde gelöscht werden:\r\n"));
            
            //SendATCommand("AT+CMGD=" + smsId);
            //ohne Rückmeldung, da nur "OK" gemeldet wird
        }

        #endregion

    }
}
