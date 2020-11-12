using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox
{
    public partial class MelSql
    {
        #region enums
        /// <summary>
        /// Gibt den Weg des Empfangs / der Sendung an. 
        /// </summary>
        [Flags]
        public enum SendToWay
        {
            None = 0,   //nicht weitersenden          
            Sms = 1,    //weiterleiten per SMS
            Email = 2   //weiterleiten per Email
        }

        public enum SendSuccess
        {
            Pending = 0,
            Unsuccessfull = 1,
            Successfull = 2
        }

        /// <summary>
        /// Kategorien für Logging
        /// </summary>
        public enum LogTopic
        {
            Allgemein,
            Start,
            Shutdown,
            Sms,
            Email,
            Sql
        }

        /// <summary>
        /// Priorisierung von Log-EInträgen (ggf später auch Meldungen )
        /// </summary>
        public enum LogPrio
        {
            Unknown,
            Error,
            Warning,
            Info
        }

        #endregion

        #region Methods

        /// <summary>
        /// Wandelt eine Zeitangabe DateTime in einen SQLite-konformen String
        /// </summary>
        /// <param name="dt">Zeit, die umgewandelt werden soll</param>
        /// <returns></returns>
        private static string SqlTime(DateTime dt)
        {
            return dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Extrahiert die ersten beiden Worte als KeyWord aus einem SMS-Text
        /// </summary>
        /// <param name="MessageContent"></param>
        /// <returns>KeyWords</returns>
        internal static string GetKeyWords(string MessageContent)
        {

            char[] split = new char[] { ' ', ',', '-', '.', ':', ';' };
            string[] words = MessageContent.Split(split);

            string KeyWords = words[0].Trim();

            if (words.Length > 1)
            {
                KeyWords += " " + words[1].Trim();
            }

            return KeyWords;
        }

        /// <summary>
        /// Gibt True aus, wenn der übergebene String dem Muster einer Email-Adresse enstricht
        /// text@text.text
        /// </summary>
        /// <param name="mailAddress"></param>
        /// <returns>True = Muster Emailadresse erfüllt.</returns>
        internal static bool IsEmail(string mailAddress)
        {
            System.Text.RegularExpressions.Regex mailIDPattern = new System.Text.RegularExpressions.Regex(@"[\w-]+@([\w-]+\.)+[\w-]+");

            if (!string.IsNullOrEmpty(mailAddress) && mailIDPattern.IsMatch(mailAddress))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Kovertiert einen String mit Zahlen und Zeichen in eine Telefonnumer als Zahl mit führender  
        /// Ländervorwahl z.B. +49 (0) 4201 123 456 oder 0421 123 456 wird zu 49421123456 
        /// </summary>
        /// <param name="str_phone">String, der eine Telefonummer enthält.</param>
        /// <returns>Telefonnumer als Zahl mit führender  
        /// Ländervorwahl (keine führende 00). Bei ungültigem str_phone Rückgabewert 0.</returns>
        public static ulong ConvertStringToPhonenumber(string str_phone)
        {
            // Entferne (0) aus +49 (0) 421...
            str_phone = str_phone.Replace("(0)", string.Empty);

            // Entferne alles ausser Zahlen
            System.Text.RegularExpressions.Regex regexObj = new System.Text.RegularExpressions.Regex(@"[^\d]");
            str_phone = regexObj.Replace(str_phone, "");

            // Wenn zu wenige Zeichen übrigbleiben gebe 0 zurück.
            if (str_phone.Length < 2) return 0;

            // Wenn am Anfang 0 steht, aber nicht 00 ersetze führende 0 durch 49
            string firstTwoDigits = str_phone.Substring(0, 2);

            if (firstTwoDigits != "00" && firstTwoDigits[0] == '0')
            {
                str_phone = "49" + str_phone.Substring(1, str_phone.Length - 1);
            }

            ulong number = ulong.Parse(str_phone);

            if (number > 0)
            {
                return number;
            }
            else
            {
                return 0;
            }
        }


        #endregion

    }
}
