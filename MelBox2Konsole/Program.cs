using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2Konsole
{
    /*
     * BESCHREIBUNG (Ziel)
     * 
     * Dieses Programm 
     * -enthält DLL für die Kommunikation mit einem GSM Modem
     * -verwaltet die SQLite-Datenbank (Da SQLite unmanaged Code enthält, ist eine eigene DLL schwieriger umzusetzen)
     * -bietet eine Kommunikationsschnittstelle für ein WPF-Programm, das ebenfalls auf die Datenbank zugreift.
     * 
     */
    partial class Program
    {
        static MelBox.MelSql sql;
        static MelBox.Gsm gsm;

        static void Main()
        {
            sql = new MelBox.MelSql(); 
            sql.RaiseSqlErrorEvent += HandleSqlErrorEvent;

            #region Test Sql, später entfernen
            sql.Log(MelBox.MelSql.LogTopic.Start, MelBox.MelSql.LogPrio.Info, "MelBox2 Neustart " + DateTime.Now);

            //sql.UpdateBlockedMessage(1, 8, 16, 7);

            //sql.UpdateCompany(1, "_UNBEKANNT2_");

            //sql.UpdateContact(3, MelSql.SendToWay.None, "", 0, "", 4915142265412);

            sql.UpdateShift(1, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1), 7);

            foreach (ulong phone in sql.GetCurrentShiftPhoneNumbers())
            {
                Console.WriteLine("aktuelle Dienstnummer: +" + phone);
            }
            #endregion


            gsm = new  MelBox.Gsm();
            gsm.RaiseGsmSystemEvent += HandleGsmSystemEvent;
            gsm.RaiseGsmRecEvent += HandleGsmRecEvent;
            gsm.RaiseGsmSentEvent += HandleGsmSentEvent;
            gsm.RaiseSmsStatusReportEvent += HandleSmsStatusReportEvent;
            gsm.RaiseSmsRecievedEvent += HandleSmsRecievedEvent;


            gsm.SetupGsm(); //Setup erst nach Event-Abbo!

            string cmdLine = "AT";
            gsm.SendATCommand(cmdLine);

            Console.WriteLine("\r\nAT-Befehl eingeben:");
            while (cmdLine.Length > 0)
            {               
                cmdLine = Console.ReadLine();
                if (cmdLine.ToLower() == "send")
                {
                    MelBox.Gsm.SmsSend(4916095285304, "MelBox2 Test " + DateTime.Now);
                    //gsm.SendMessage(4916095285304, "MelBox2 Test " + DateTime.Now);
                }
                else
                {
                    gsm.SendATCommand(cmdLine);
                }
            }

            gsm.ClosePort();
        }
    }
}
