using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace MelBox
{
    public partial class MelSql
    {
        #region Fields
        //Connect-String für Datenbankverbindung
        private readonly string Datasource = "Data Source=" + DbPath;

        internal const int ContactIdBereitschaftshandy = 3;
        #endregion

        #region Properties
        //Pfad zur Datenbank-Datei; Standartwert: Unterordner "DB" dieser exe
        public static string DbPath { get; set; } = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "DB", "MelBox2.db");

        #endregion

        #region Methods

        public MelSql()
        {            
            //Datenbak prüfen / erstellen
            if (!System.IO.File.Exists(DbPath))
            {
                CreateNewDataBase();
            }
        }

        /// <summary>
        /// Erzeugt eine neue Datenbankdatei, erzeugt darin Tabellen, Füllt diverse Tabellen mit Defaultwerten.
        /// </summary>
        private void CreateNewDataBase()
        {
            //Erstelle Datenbank-Datei und öffne einmal 
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DbPath));
            FileStream stream = File.Create(DbPath);
            stream.Close();

            //Erzeuge Tabellen in neuer Datenbank-Datei
            //Zeiten im Format TEXT (Lesbarkeit Rohdaten)
            using (SQLiteConnection con = new SQLiteConnection(Datasource))
            {
                con.Open();

                List<String> TableCreateQueries = new List<string>
                    {
                        //Debug Log
                        "CREATE TABLE \"Log\"(\"Id\" INTEGER NOT NULL PRIMARY KEY UNIQUE,\"LogTime\" TEXT NOT NULL, \"Topic\" TEXT , \"Prio\" INTEGER NOT NULL, \"Content\" TEXT);",

                        //Kontakte
                        "CREATE TABLE \"Company\" (\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NOT NULL, \"Address\" TEXT, \"City\" TEXT); ",

                        //"INSERT INTO \"Company\" (\"Id\", \"Name\", \"Address\", \"City\") VALUES (0, '_UNBEKANNT_', 'Musterstraße 123', '12345 Modellstadt' );",
                        //"INSERT INTO \"Company\" (\"Id\", \"Name\", \"Address\", \"City\") VALUES (1, 'Kreutzträger Kältetechnik GmbH & Co. KG', 'Theodor-Barth-Str. 21', '28307 Bremen' );",

                        "CREATE TABLE \"Contact\"(\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE, \"EntryTime\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"Name\" TEXT NOT NULL, " +
                        "\"CompanyId\" INTEGER, \"Email\" TEXT, \"Phone\" INTEGER, \"KeyWord\" TEXT, \"MaxInactiveHours\" INTEGER DEFAULT 0, \"SendWay\" INTEGER DEFAULT 0);",

                        //"INSERT INTO \"Contact\" (\"Id\", \"Time\", \"Name\", \"CompanyId\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (1, CURRENT_TIMESTAMP, 'SMSZentrale', 1, 'smszentrale@kreutztraeger.de', 4915142265412," + SendToWay.None + ");",
                        //"INSERT INTO \"Contact\" (\"Id\", \"Time\", \"Name\", \"CompanyId\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (2, CURRENT_TIMESTAMP, 'MelBox2Admin', 1, 'harm.schnakenberg@kreutztraeger.de', 0," + (SendToWay.Sms & SendToWay.Email ) + ");",
                        //"INSERT INTO \"Contact\" (\"Id\", \"Time\", \"Name\", \"CompanyId\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (3, CURRENT_TIMESTAMP, 'Bereitschaftshandy', 1, 'bereitschaftshandy@kreutztraeger.de', 9999491728362586," + SendToWay.None + ");",
                        //"INSERT INTO \"Contact\" (\"Id\", \"Time\", \"Name\", \"CompanyId\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (4, CURRENT_TIMESTAMP, 'Kreutzträger Service', 1, 'service@kreutztraeger.de', 0," +  SendToWay.None + ");",
                        //"INSERT INTO \"Contact\" (\"Id\", \"Time\", \"Name\", \"CompanyId\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (5, CURRENT_TIMESTAMP, 'Henry Kreutzträger', 1, 'henry.kreutztraeger@kreutztraeger.de', 491727889419," + SendToWay.None + ");",
                        //"INSERT INTO \"Contact\" (\"Id\", \"Time\", \"Name\", \"CompanyId\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (6, CURRENT_TIMESTAMP, 'Bernd Kreutzträger', 1, 'bernd.kreutztraeger@kreutztraeger.de', 491727875067," + SendToWay.None + ");",

                        //Nachrichten
                        "CREATE TABLE \"MessageContent\" (\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"Content\" TEXT NOT NULL UNIQUE );",
                        //"INSERT INTO \"MessageContent\" (\"Content\") VALUES ('Datenbank neu erstellt.');",

                        "CREATE TABLE \"LogRecieved\"( \"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"RecieveTime\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"FromContactId\" INTEGER NOT NULL, \"ContentId\" INTEGER NOT NULL);",

                        //"INSERT INTO \"LogRecieved\" (\"RecieveTime\", \"FromContactId\", \"ContentId\") VALUES " +
                        //"( CURRENT_TIMESTAMP, 0, 1, 1);",

                        "CREATE TABLE \"LogSent\" (\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"LogRecievedId\" INTEGER NOT NULL, \"SentTime\" TEXT NOT NULL, \"SentToId\" INTEGER NOT NULL, \"SentVia\" INTEGER NOT NULL, \"ConfirmStatus\" INTEGER NOT NULL DEFAULT 0);" +
                        //"INSERT INTO \"LogSent\" (\"LogRecievedId\", \"SentTime\", \"SentToId\", \"SentVia\") VALUES " +
                        //"(1, CURRENT_TIMESTAMP, 1, " + SendToWay.Email + ");",

                        //Bereitschaft
                        "CREATE TABLE \"Shifts\"( \"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"EntryTime\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, " +
                        "\"ContactId\" INTEGER NOT NULL, \"StartTime\" TEXT NOT NULL, \"EndTime\" TEXT NOT NULL );",

                        //"INSERT INTO \"Shifts\" (\"EntryTime\", \"ContactId\", \"StartTime\", \"EndTime\") VALUES " +
                        //"(CURRENT_TIMESTAMP, 1, CURRENT_TIMESTAMP, date('now','end of week') )",

                        "CREATE TABLE \"BlockedMessages\"( \"Id\" INTEGER NOT NULL UNIQUE, \"EntryTime\" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, \"StartHour\" TEXT NOT NULL, " +
                        "\"EndHour\" TEXT NOT NULL, \"Days\" INTEGER NOT NULL CHECK (\"Days\" < 10));"

                        //"INSERT INTO \"BlockedMessages\" (\"Id\", \"StartHour\", \"EndHour\", \"WorkdaysOnly\" ) VALUES " +
                        //"(1,8,8,0);"
                };

                foreach (string query in TableCreateQueries)
                {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                    using (var SQLiteCommand = new SQLiteCommand(query, con))
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                    {

                        using (SQLiteCommand cmd = SQLiteCommand)
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                InsertCompany("_UNBEKANNT_", "Musterstraße 123", "12345 Modellstadt");
                InsertCompany("Kreutzträger Kältetechnik GmbH & Co. KG", "Theodor-Barth-Str. 21", "28307 Bremen");

                InsertContact("SMSZentrale", 1, "smszentrale@kreutztraeger.de", 4915142265412, SendToWay.None);
                InsertContact("MelBox2Admin", 1, "harm.schnakenberg@kreutztraeger.de", 0, SendToWay.Sms & SendToWay.Email);
                InsertContact("Bereitschaftshandy", 1, "bereitschaftshandy@kreutztraeger.de", 491728362586, SendToWay.None);
                InsertContact("Kreutzträger Service", 1, "service@kreutztraeger.de", 0, SendToWay.None);
                InsertContact("Henry Kreutzträger", 1, "henry.kreutztraeger@kreutztraeger.de", 491727889419, SendToWay.None);
                InsertContact("Bernd Kreutzträger", 1, "bernd.kreutztraeger@kreutztraeger.de", 491727875067, SendToWay.None);
                InsertContact("Harm privat", 1, "harm.schnakenberg@kreutztraeger.de", 4916095285304, SendToWay.Sms);

                InsertMessage("Datenbank neu erstellt.", 0, "smszentrale@kreutztraeger.de");

                InsertLogSent(1, 1, SendToWay.None);

                InsertShift(2, DateTime.Now, DateTime.Now.AddDays(2));

                InsertBlockedMessage(1);
            }

            #endregion
        }

    }
}
