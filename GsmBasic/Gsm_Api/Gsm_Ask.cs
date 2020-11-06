using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MelBox
{
    public partial class Gsm
    {
        public void SetupGsm()
        {
            Thread.Sleep(500);
            //Textmode
            SendATCommand("AT+CMGF=1");
            Thread.Sleep(500);

            SendATCommand("AT+CPMS=\"SM\"");
            //SendATCommand("AT+CPMS=\"SM\",\"SM\",\"SM\"");
            Thread.Sleep(500);

            //Erzwinge, dass bei Fehlerhaftem senden "+CMS ERROR: <err>" ausgegeben wird
            SendATCommand("AT^SM20=0,0");
            Thread.Sleep(500);

            //SIM-Karte im Mobilfunknetz registriert?
            SendATCommand("AT+CREG?");
            Thread.Sleep(500);

            //Signalqualität
            SendATCommand("AT+CSQ");
            Thread.Sleep(500);

            //Sendeempfangsbestätigungen abonieren
            //Quelle: https://www.codeproject.com/questions/271002/delivery-reports-in-at-commands
            //Quelle: https://www.smssolutions.net/tutorials/gsm/sendsmsat/
            SendATCommand("AT+CSMP=49,1,0,0");
            Thread.Sleep(500);
            SendATCommand("AT+CNMI=2,1,2,2,1");
            Thread.Sleep(500);

        }

        #region SMS verschicken
        /// <summary>
        /// 
        /// </summary>
        /// <param name="phone">Format mit Ländervorwahl: 49123456789</param>
        /// <param name="content">SMS-Text; Zeilenumbrüche werden entfernt.</param>
        /// <returns>ID der gesendeten SMS</returns>
        public void SendMessage(ulong phone, string content)
        {
            OnRaiseGsmSystemEvent(new GsmEventArgs(11051543, "Sende an: " + phone + " - " + content));

            const string ctrlz = "\u001a";
            content = content.Replace("\r\n", " ");
            if (content.Length > 160) content = content.Substring(0, 160);
 
            SendATCommand("AT+CMGS=\"+" + phone + "\"\r");
            Thread.Sleep(200); //Angstpause
            SendATCommand(content + ctrlz);
        }

        #endregion

    }
}
