using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace MelBox
{
    public partial class MelSql
    {
        /// <summary>
        /// Neuer Log-Eintrag
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="prio"></param>
        /// <param name="content"></param>
        public void Log(LogTopic topic, LogPrio prio, string content)
        {
            try
            {
                const string query = "INSERT INTO Log(LogTime, Topic, Prio, Content) VALUES (CURRENT_TIMESTAMP, @topic, @prio, @content)";

                var args = new Dictionary<string, object>
                {
                    {"@time", "" },
                    {"@topic", topic.ToString() },
                    {"@prio", (ushort)prio},
                    {"@content", content}
                };

                using (SQLiteConnection con = new SQLiteConnection(Datasource))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            //catch (DllNotFoundException dll_ex)
            //{
            //    Console.WriteLine(dll_ex.Message + "\r\n" + dll_ex.InnerException);
            //    Console.ReadKey();
            //}
            catch (Exception ex)
            {
                OnRaiseSqlErrorEvent("Log()", ex);
                Console.WriteLine(ex.Message);
                throw new Exception("Sql-Fehler Log() " + ex.GetType().Name);
            }
        }

        /// <summary>
        /// Neuer Eintrag für Unternehmen
        /// </summary>
        /// <param name="name">Anzeigename des Unternehmens</param>
        /// <param name="address">Standortadresse</param>
        /// <param name="city">PLZ, Ort</param>
        public void InsertCompany(string name, string address, string city)
        {
            try
            {
                const string query = "INSERT INTO \"Company\" (\"Name\", \"Address\", \"City\") VALUES (@name, @address, @city );";

                var args = new Dictionary<string, object>
                {
                    {"@name", name },
                    {"@address", address},
                    {"@city", city},
                };

                using (SQLiteConnection con = new SQLiteConnection(Datasource))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                OnRaiseSqlErrorEvent("InsertCompany()", ex);
                throw new Exception("Sql-Fehler InsertCompany()");
            }
        }

        /// <summary>
        /// Neuer Eintag für einen Kontakt (Sender / Empfänger) 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="companyId"></param>
        /// <param name="email"></param>
        /// <param name="phone"></param>
        /// <param name="sendWay"></param>
        public void InsertContact(string name, int companyId, string email, ulong phone, SendToWay sendWay)
        {
            try
            {
                const string query = "INSERT INTO \"Contact\" (\"Name\", \"CompanyId\", \"Email\", \"Phone\", \"SendWay\" ) VALUES (@name, @companyId, @email, @phone, @sendWay);";

                var args = new Dictionary<string, object>
                {
                    {"@name", name },
                    {"@companyId", companyId},
                    {"@email", email},
                    {"@phone", phone},
                    {"@sendWay", (int)sendWay},
                };

                using (SQLiteConnection con = new SQLiteConnection(Datasource))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                OnRaiseSqlErrorEvent("InsertContact()", ex);
                throw new Exception("Sql-Fehler InsertContact()");
            }
        }

        /// <summary>
        /// Schreibt eine neue Nachricht in die Datenbank
        /// Indiziert den Inhalt, protokollietr den Empfang
        /// </summary>
        /// <param name="message">Inhalt der Nachricht</param>
        /// <param name="phone">von Telefon</param>
        /// <param name="email">von Email</param>
        public uint InsertMessage(string message, ulong phone = 0, string email = "")
        {
            try
            {
                //Absender identifizieren
                int senderId = GetContactId("", phone, email, message);

                //Inhalt identifizieren
                uint msgId = GetMessageId(message);

                const string query = "INSERT INTO \"LogRecieved\" (\"RecieveTime\", \"FromContactId\", \"ContentId\") VALUES " +
                                     "( CURRENT_TIMESTAMP, @fromContactId, @contentId);";

                var args = new Dictionary<string, object>
                {
                    {"@fromContactId", senderId },
                    {"@contentId", msgId},
                };

                using (SQLiteConnection con = new SQLiteConnection(Datasource))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }

                return msgId;
            }
            catch (Exception ex)
            {
                OnRaiseSqlErrorEvent("InsertMessage()", ex);
                //throw new Exception("Sql-Fehler InsertMessage()" + ex.GetType() + "\r\n" + ex.Message );
                return 0;
            }
        }

        /// <summary>
        /// Protokolliert die Weiterleitung einer Nachricht
        /// </summary>
        /// <param name="recMsgId">Id der Nachricht, die weitergeleitet wurde</param>
        /// <param name="sentToId">Id des Kontakts, an den die Nachricht gesendet wurde</param>
        /// <param name="way">Sendeweg (SMS, Email)</param>
        public void InsertLogSent(int recMsgId, int sentToId, SendToWay way)
        {
            try
            {
                const string query = "INSERT INTO \"LogSent\" (\"LogRecievedId\", \"SentTime\", \"SentToId\", \"SentVia\") " +
                                     "VALUES (@msgId, CURRENT_TIMESTAMP, @sentToId, @sendWay);";

                var args = new Dictionary<string, object>
                {
                    {"@msgId", recMsgId},
                    {"@sentToId", sentToId},
                    {"@sendWay", (int)way},
                };

                using (SQLiteConnection con = new SQLiteConnection(Datasource))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                OnRaiseSqlErrorEvent("InsertLogSent()", ex);
                throw new Exception("Sql-Fehler InsertLogSent()");
            }
        }

        /// <summary>
        /// Erstellt einen neuen Bereitschaftsdienst
        /// </summary>
        /// <param name="contactId">Id des Bereitschaftsnehmers</param>
        /// <param name="startTime">Beginn der Bereitschaft</param>
        /// <param name="endTime">Ende der Bereitschaft</param>
        public void InsertShift(int contactId, DateTime startTime, DateTime endTime)
        {
            try
            {
                const string query = "INSERT INTO \"Shifts\" (\"EntryTime\", \"ContactId\", \"StartTime\", \"EndTime\") VALUES " +
                                     "(CURRENT_TIMESTAMP, @contactId, @startTime, @endTime )";

                var args = new Dictionary<string, object>
                {
                    {"@contactId", contactId},
                    {"@startTime", SqlTime(startTime)},
                    {"@endTime", SqlTime(endTime)},
                };

                using (SQLiteConnection con = new SQLiteConnection(Datasource))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                OnRaiseSqlErrorEvent("InsertShift()", ex);
                throw new Exception("Sql-Fehler InsertShift()");
            }
        }

        /// <summary>
        /// Fügt eine Nachricht der "Blacklist" hinzu, sodass diese bei Empfang nicht weitergeleitet wird.
        /// Ist die Nachricht bereits in der Liste vorhanden, wird kein neuer Eintrag erstellt.
        /// </summary>
        /// <param name="msgId">Id der Nachricht, deren Weiterleitung gesperrt werden soll</param>
        /// <param name="startHour">Tagesstunde - Beginn der Sperre</param>
        /// <param name="endHour">Tagesstunde - Ende der Sperre</param>
        /// <param name="Days">Tage, an denen die Nachricht gesperrt sein soll, wie Wochenuhr; Mo=1, Di=2,...,Werktags=8, Alle Tage=9</param>
        public void InsertBlockedMessage(int msgId, int days = 9, int startHour = 17, int endHour = 7)
        {
            try
            {
                //Nur neuen Eintrag erzeugen, wenn msgId noch nicht vorhanden ist.
                const string query = "INSERT OR IGNORE INTO \"BlockedMessages\" (\"Id\", \"StartHour\", \"EndHour\", \"Days\" ) VALUES " +
                                     "(@msgId, @startHour, @endHour, @days)";
                //"FROM \"BlockedMessages\" WHERE NOT EXISTS " +
                //"(SELECT \"Id\" FROM \"BlockedMessages\" WHERE \"Id\" = @msgId)";

                var args = new Dictionary<string, object>
                {
                    {"@msgId", msgId},
                    {"@startHour", startHour},
                    {"@endHour", endHour},
                    {"@days", days}
                };

                using (SQLiteConnection con = new SQLiteConnection(Datasource))
                {
                    con.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
                    {
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                OnRaiseSqlErrorEvent("InsertBlockedMessage()", ex);
                throw new Exception("Sql-Fehler InsertBlockedMessage()\r\n" + ex.Message);
            }
        }

    }
}
