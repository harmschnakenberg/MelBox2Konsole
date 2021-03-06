﻿using MelBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox2Konsole
{
	partial class Program
	{
        /*
         * Hier stehen Reaktionen auf Ereignisse aus Handling mit GSM oder SQL 
         * 
         */
       
        static void HandleSqlErrorEvent(object sender, MelBox.SqlErrorEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("SQL: " + e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

		static void HandleGsmSystemEvent(object sender, GsmEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.WriteLine(e.Id + ": " + e.Message);
			Console.ForegroundColor = ConsoleColor.Gray;
		}

		static void HandleGsmSentEvent(object sender, GsmEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine(e.Id + ": " + e.Message);
			Console.ForegroundColor = ConsoleColor.Gray;
		}

		static void HandleGsmRecEvent(object sender, GsmEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine(e.Id + ": " + e.Message);
			Console.ForegroundColor = ConsoleColor.Gray;
		}

		static void HandleSmsStatusReportEvent(object sender, GsmStatusReportEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(string.Format("SMS konnte {0} zugestellt werden:\r\nAn: {1}\r\n{2}", e.SendSuccess < 32 ? "erfolgreich" : "nicht" ,e.Phone ,e.Message));
			Console.ForegroundColor = ConsoleColor.Gray;

			sql.UpdateSmsSendStatus(e.Phone, e.Message, e.SendSuccess);
		}

		static void HandleSmsRecievedEvent(object sender, ShortMessageArgs e)
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("SMS empfangen:\r\n" + e.Sender + ": " + e.Message);
			
			ulong phone = MelSql.ConvertStringToPhonenumber(e.Sender);
			if (phone == 0)
            {
				//Telefonnummer konnte nicht entziffert werden!
				Console.WriteLine("Die Telefonnumer " + e.Sender + " konnte nicht gelesen werden.");
			}
			Console.ForegroundColor = ConsoleColor.Gray;

			Gsm.SmsSendMulti( sql.SafeAndRelayMessage(e.Message, phone) );

		}

		static void HandleSmsSentEvent(object sender, ShortMessageArgs e)
        {
			ulong phoneTo = ulong.Parse(e.Sender.Trim().Trim('+'));

			//SMS in DB eintragen
			sql.InsertLogSent(e.Message, phoneTo);
        }

	}
}
