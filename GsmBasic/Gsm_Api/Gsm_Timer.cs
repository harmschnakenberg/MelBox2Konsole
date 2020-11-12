using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace MelBox
{
	public partial class Gsm
	{
        #region Fields

        #endregion

        #region Properties
        /// <summary>
        /// Liste der gesendeten Nachrichten ohne Empfangsbestätigung mit Anzahl Sendeversuchen
        /// </summary>
        private static readonly Dictionary<ShortMessageArgs, int> SendRetrys = new Dictionary<ShortMessageArgs, int>();

        /// <summary>
        /// Anzahl maximaler Sendewiederholungsversuche für SMS
        /// </summary>
        public static int MaxSendRetrys { get; set; } = 5;

        /// <summary>
        /// Zeit zwischen Sendewiederholungsversuchen
        /// </summary>
        public static int MinutesToSendRetry { get; set; } = 2;

        #endregion

        #region Events aus Timer

        /// <summary>
        /// Event 'string empfangen von COM'
        /// </summary>
        public event EventHandler<GsmStatusReportEventArgs> RaiseSmsStatusReportEvent;

        /// <summary>
        /// Triggert das Event 'string empfangen von COM'
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRaiseSmsTimeoutEvent(GsmStatusReportEventArgs e)
        {
            RaiseSmsStatusReportEvent?.Invoke(this, e);
        }

        #endregion

        #region Methods

        #region Senden und empfangen zeitlich ordnen

        /// <summary>
        /// 
        /// </summary>
        internal void SetCyclicTimer()
        {
            Loop();
            System.Timers.Timer aTimer = new System.Timers.Timer(60000); //1 min
            aTimer.Elapsed += (sender, eventArgs) => Loop();
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        #endregion

        #region Bei Sendefehlschlag SMS erneut versenden
        /// <summary>
        /// Wird beim Senden einer SMS aufgerufen und startet deren Nachverfolgung
        /// </summary>
        /// <param name="sms"></param>
        internal void SetRetrySendSmsTimer(ShortMessageArgs msg)
        {
            OnRaiseGsmSystemEvent(new GsmEventArgs(11111752, "merke mir SMS " + msg.Index + " für Nachverfolgung.")); ;

            //Wenn Nachricht nicht in Wiederholungsliste steht, hinzufügen
            if (!SendRetrys.Keys.Contains(msg))
                SendRetrys.Add(msg, 0);
            
            //Warte bis zum Wiederholungsversuch
            System.Timers.Timer aTimer = new System.Timers.Timer(MinutesToSendRetry * 60000); //2 min
            aTimer.Elapsed += (sender, eventArgs) => OnRetrySendSms(msg);
            aTimer.AutoReset = false;
            aTimer.Enabled = true;
        }

        /// <summary>
        /// Wiederholter Sendeversuch einer SMS
        /// </summary>
        /// <param name="sms"></param>
        private void OnRetrySendSms(ShortMessageArgs msg)
        {
            //Falls Nachricht noch in der Sendeliste ansteht, nichts unternehmen 
            //unwahrscheinlich, dass dies gebraucht wird-
            //if (SendQueue.Contains(sms)) return;

            //Falls Nachricht nicht (mehr) in der Wiederholungsliste ansteht, nichts unternehmen
            if (!SendRetrys.Keys.Contains(msg)) return;

            //Zähler Sendeversuche hochsetzen
            ++SendRetrys[msg];
            ulong phone = ulong.Parse(msg.Sender);

            if (SendRetrys[msg] > MaxSendRetrys)
            {
                //Maximale Sendeversuche überschritten.                 
                OnRaiseSmsTimeoutEvent(new GsmStatusReportEventArgs(phone, msg.Message, false));
                SendRetrys.Remove(msg);
            }
            else
            {
                //SMS wieder in die Sendeliste eintragen
                SmsSend(phone, msg.Message);
            }
        }
        #endregion

        #endregion
    }

}
