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
        /// <summary>
        /// Ids von rausgegangenen SMS, für die noch keine EMpfangsbestätigung erhalten wurde.
        /// TODO: Ids die länger in dieser Liste bleiben erneut senden.
        /// </summary>
        private static readonly List<int> ConfirmedSentSms = new List<int>();

       // private static System.Timers.Timer aTimer;
        #endregion


        /// <summary>
        /// Startet einen Timer, nach dessen Ablauf geprüft wird, ob eine Empfangsbestätigung vorliegt
        /// </summary>
        /// <param name="id"></param>
        internal void SetStatusReportTimer(int id)
        {
            System.Timers.Timer aTimer = new System.Timers.Timer(300000); //5 min
            aTimer.Elapsed += (sender, eventArgs) => OnTimedEvent(id); 
            aTimer.AutoReset = false;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(int id)
        {
            if (ConfirmedSentSms.Contains(id))
            {
                ConfirmedSentSms.Remove(id);
                //Empfänger hat Empfang von SMS mit id bestätigt
                //TODO: Bestätigung veröffentlichen -> Empfangsbestätigung in Datenbank vermerken.

                //TODO: SMS mit id im SIM löschen.
            }
            else
            {
                //Keine passende Empfangsbestätigung gefunden
                //TODO: Fehlende Bestätigung veröffentlichen -> nächsten Sender probieren?
            }

        }


      



    }
}
