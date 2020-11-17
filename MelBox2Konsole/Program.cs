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
            gsm.RaiseSmsSentEvent += HandleSmsSentEvent;
            //gsm.Port.ErrorReceived += 

            gsm.SetupGsm(); //Setup erst nach Event-Abbo!


            string cmdLine = "AT";
            gsm.SendATCommand(cmdLine);

            Console.WriteLine("\r\nAT-Befehl eingeben:");
            while (cmdLine.Length > 0)
            {               
                cmdLine = Console.ReadLine();
                if (cmdLine.ToLower().StartsWith(">"))
                {
                    //>send >+49123456789 >Dies ist eine SMS-Nachricht
                    if (cmdLine.ToLower().StartsWith(">send"))
                    {
                        string[] msg = cmdLine.Split('>');
                        ulong.TryParse(msg[1].Trim(), out ulong phone);
                        if (phone > 0 && msg.Length > 2)
                        MelBox.Gsm.SmsSend(phone, msg[2]);
                    }
                    if (cmdLine.ToLower().StartsWith(">testrec"))
                    {
                        MelBox.ShortMessageArgs args = new MelBox.ShortMessageArgs
                        {
                            Index = "99",
                            Sender = "+4915142265412",
                            Message = "Simuliert SMS Empfang"
                        };
                        HandleSmsRecievedEvent(null, args);
                    }
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
