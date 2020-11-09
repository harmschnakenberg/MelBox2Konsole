using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MelBox
{
    public partial class Gsm
    {

        #region Properties
        internal static List<Tuple<ulong, string>> SendQueue { get; set; } = new List<Tuple<ulong, string>>();
        #endregion

        public void SetupGsm()
        {
            RaiseGsmRecEvent += InterpretGsmRecEvent;

            //Textmode
            SendATCommand("AT+CMGF=1");

            //SendATCommand("AT+CPMS=\"SM\"");
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
            SetSendSmsTimer();
        }

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

        #region SMS verschicken

        /// <summary>
        /// Wird intervallweise getriggert, durchläuft einen kompletten Empfangs-/Sende-Zyklus
        /// </summary>
        private void Loop()
        {
            //1) anstehende SMS auf SIM speichern 'AT+CMGW'
            SendAllMessages();

            //2) alle SMS lesen 'AT+CMGL="ALL"'

            //3)   neu erhaltene SMS verarbeiten "REC UNREAD" + "REC READ"  
            //     nach Schreiben in DB aus SIM löschen. 
            //3.1) Sendebestätigungen verarbeiten "REC UNREAD"
            //     nach Bestätigung oder Sendeversuchüberschreitung aus Bestätigung und SMS "STO SENT" aus SIM löschen
            //3.2) Zu sendende SMS aus Speicher senden "STO UNSENT" => "AT+CMSS"
            //     
            //5) optional: erfrage Mobilfunknetzqualität
        }

        public static void AddSendSms(ulong phone, string content)
        {
            Tuple<ulong, string> t = new Tuple<ulong, string>(phone, content);
            if (!SendQueue.Contains(t))
                SendQueue.Add(t);
        }


        /// <summary>
        /// Sendet alle in SendQueue anstehenden SMS
        /// </summary>
        /// <param name="phone">Format mit Ländervorwahl: 49123456789</param>
        /// <param name="content">SMS-Text; Zeilenumbrüche werden entfernt.</param>
        /// <returns>ID der gesendeten SMS</returns>
        private void SendAllMessages()
        {
            if (SendQueue.Count < 1) return;

            foreach(Tuple<ulong,string> sms in SendQueue)
            {
                ulong phone = sms.Item1;
                string content = sms.Item2;
                SetRetrySendSmsTimer(sms);

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


    }
}
