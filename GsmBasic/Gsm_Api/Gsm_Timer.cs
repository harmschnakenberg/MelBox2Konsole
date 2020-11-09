using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MelBox
{
	public partial class Gsm
	{
        #region Fields

        #endregion

        #region Properties

        private static readonly Dictionary<Tuple<ulong, string>, int> SendRetrys = new Dictionary<Tuple<ulong, string>, int>();

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
        public event EventHandler<GsmSmsTimeoutEventArgs> RaiseSmsTimeoutEvent;

        /// <summary>
        /// Triggert das Event 'string empfangen von COM'
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRaiseSmsTimeoutEvent(GsmSmsTimeoutEventArgs e)
        {
            RaiseSmsTimeoutEvent?.Invoke(this, e);
        }

        #endregion

        #region Methods

        #region Sende regelmäßig SMS
        /// <summary>
        /// Startet einen Timer, nach dessen Ablauf wiederholt geprüft wird, ob neue SMS empfangen wurden
        /// </summary>
        /// <param name="id"></param>
        internal void SetSendSmsTimer()
        {
            Loop(); //Einmal ausführen, bevor Timer abgelaufen ist

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
        internal void SetRetrySendSmsTimer(Tuple<ulong, string> sms)
        {
            //Wenn Nachricht nicht in Wiederholungsliste steht, hinzufügen
            if (!SendRetrys.Keys.Contains(sms))
                SendRetrys.Add(sms, 0);
            
            //Warte bis zum Wiederholungsversuch
            System.Timers.Timer aTimer = new System.Timers.Timer(MinutesToSendRetry * 60000); //2 min
            aTimer.Elapsed += (sender, eventArgs) => OnRetrySendSms(sms);
            aTimer.AutoReset = false;
            aTimer.Enabled = true;
        }

        /// <summary>
        /// Wiederholter Sendeversuch einer SMS
        /// </summary>
        /// <param name="sms"></param>
        private void OnRetrySendSms(Tuple<ulong, string> sms)
        {
            //Falls Nachricht noch in der Sendeliste ansteht, nichts unternehmen
            if (SendQueue.Contains(sms)) return;

            //Falls Nachricht nicht (mehr) in der Wiederholungsliste ansteht, nichts unternehmen
            if (!SendRetrys.Keys.Contains(sms)) return;

            //Zähler Sendeversuche hochsetzen
            ++SendRetrys[sms];

            if (SendRetrys[sms] > MaxSendRetrys)
            {
                //Maximale Sendeversuche überschritten. 
                OnRaiseSmsTimeoutEvent(new GsmSmsTimeoutEventArgs(sms.Item1, sms.Item2));
                SendRetrys.Remove(sms);
            }
            else
            {
                //SMS wieder in die Sendeliste eintragen
                SendQueue.Add(sms);
            }
        }
        #endregion

        #endregion
    }

    /// <summary>
    /// EventArgs bei Überschreiten der maximalen Sendeversuche einer SMS
    /// </summary>
    public class GsmSmsTimeoutEventArgs : EventArgs
    {
        public GsmSmsTimeoutEventArgs(ulong phone, string message)
        {
            Phone = phone;
            Message = message;
        }

        public ulong Phone { get; set; }
        public string Message { get; set; }
    }
}
