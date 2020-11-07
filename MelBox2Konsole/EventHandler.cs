using MelBox;
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
			Console.ForegroundColor = ConsoleColor.Black;
		}

		static void HandleGsmSentEvent(object sender, GsmEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine(e.Id + ": " + e.Message);
			Console.ForegroundColor = ConsoleColor.Black;
		}

		static void HandleGsmRecEvent(object sender, GsmEventArgs e)
		{
			Console.ForegroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine(e.Id + ": " + e.Message);
			Console.ForegroundColor = ConsoleColor.Black;
		}
	}
}
