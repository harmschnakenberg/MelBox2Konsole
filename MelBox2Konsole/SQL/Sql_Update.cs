using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace MelBox
{
    public partial class MelSql
    {

        /// <summary>
        /// Ändert den Eintrag einer Firma.
        /// Übergebene Parameter mit Leerstring werden nicht gegändert.
        /// </summary>
        /// <param name="companyId">Id der Firma</param>
        /// <param name="name">neuer Name der Firma (sonst leer)</param>
        /// <param name="address">neue Anschrift der Firma (sonst leer)</param>
        /// <param name="city">neuer Ort der Firma, ggf. mit PLZ (sonst leer)</param>
        public void UpdateCompany(int companyId, string name = "", string address = "", string city = "")
        {
            try
            {
                string query = string.Empty;
                var args = new Dictionary<string, object>
                {
                    { "@companyId", companyId }
                };

                if (name.Length > 3)
                {
                    query += "UPDATE \"Company\" SET \"Name\" = @name WHERE \"Id\" = @companyId;";
                    args.Add("@name", name);
                }

                if (address.Length > 3)
                {
                    query += "UPDATE \"Company\" SET \"Address\" = @address WHERE \"Id\" = @companyId;";
                    args.Add("@address", name);
                }

                if (city.Length > 3)
                {
                    query += "UPDATE \"Company\" SET \"Address\" = @address WHERE \"Id\" = @companyId;";
                    args.Add("@address", name);
                }

                if (query.Length < 1) return;

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
            catch
            {
                throw new Exception("Sql-Fehler UpdateCompany()");
            }
        }

        /// <summary>
        /// Ändert den Eintrag für einen Kontakt.
        /// Übergebene Parameter mit Leerstring werden nicht gegändert.
        /// </summary>
        /// <param name="contactId"></param>
        /// <param name="sendWay">Sendeweg (Achtung: Wird immer aktualisiert!)</param>
        /// <param name="name">Anzeigename</param>
        /// <param name="companyId">Id der Firma</param>
        /// <param name="email"></param>
        /// <param name="phone"></param>
        /// <param name="keyWord">Leerstring wird ignoriert</param>
        public void UpdateContact(int contactId, SendToWay sendWay, string name = "", int companyId = 0, string email = "", ulong phone = 0, string keyWord = "")
        {
            try
            {
                string query = string.Empty;
                var args = new Dictionary<string, object>
                {
                    { "@contactId", contactId }
                };

                //nicht gut: SendWay wird immer aktualisiert
                query += "UPDATE \"Contact\" SET \"SendWay\" = @sendWay WHERE \"Id\" = @contactId;";
                args.Add("@sendWay", (int)sendWay);

                if (name.Length > 3)
                {
                    query += "UPDATE \"Contact\" SET \"Name\" = @name WHERE \"Id\" = @contactId;";
                    args.Add("@name", name);
                }

                if (companyId > 0)
                {
                    query += "UPDATE \"Contact\" SET \"CompanyId\" = @companyId WHERE \"Id\" = @contactId;";
                    args.Add("@companyId", companyId);
                }

                if (IsEmail(email))
                {
                    query += "UPDATE \"Contact\" SET \"Email\" = @email WHERE \"Id\" = @contactId;";
                    args.Add("@email", email);
                }

                if (phone > 0)
                {
                    query += "UPDATE \"Contact\" SET \"Phone\" = @phone WHERE \"Id\" = @contactId;";
                    args.Add("@phone", phone);
                }

                if (keyWord == null) //kein KeyWord 
                {
                    query += "UPDATE \"Contact\" SET \"KeyWord\" = NULL WHERE \"Id\" = @contactId;";
                }
                else if (keyWord.Length > 0) //Leerstring als KeyWord nicht zulässig                
                {
                    query += "UPDATE \"Contact\" SET \"KeyWord\" = @keyWord WHERE \"Id\" = @contactId;";
                    args.Add("@keyWord", keyWord);
                }
            
                if (query.Length < 1) return;

                using (SQLiteConnection con = new SQLiteConnection(Datasource))
                {
                    con.Open();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                    using (SQLiteCommand cmd = new SQLiteCommand(query, con))
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                    {
                        foreach (var pair in args)
                        {
                            cmd.Parameters.AddWithValue(pair.Key, pair.Value);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                throw new Exception("Sql-Fehler UpdateContact()");
            }
        }

        /// <summary>
        /// Ändert den Eintrag für einen Bereitschaftsdienst
        /// </summary>
        /// <param name="shiftId">Id des Bereitschaftsdiensts</param>
        /// <param name="startTime">Beginn der Bereitschaft, bei DateTime.MinValue keine Änderung</param>
        /// <param name="endTime">Ende der Bereitschaft, bei DateTime.MinValue keine Änderung</param>
        /// <param name="contactId">Id des Bereitschftnehmers, bei 0 keine Änderung</param>
        public void UpdateShift(int shiftId, DateTime startTime, DateTime endTime, int contactId = 0)
        {
            try
            {
                string query = string.Empty;
                var args = new Dictionary<string, object>
                {
                    { "@shiftId", shiftId }
                };

                if (contactId > 0)
                {
                    query += "UPDATE \"Shifts\" SET \"ContactId\" = @contactId WHERE \"Id\" = @shiftId;";
                    args.Add("@contactId", contactId);
                }

                if (startTime > DateTime.MinValue)
                {
                    query += "UPDATE \"Shifts\" SET \"StartTime\" = @startTime WHERE \"Id\" = @shiftId;";
                    args.Add("@startTime", startTime);
                }

                if (endTime > DateTime.MinValue)
                {
                    query += "UPDATE \"Shifts\" SET \"EndTime\" = @endTime WHERE \"Id\" = @shiftId;";
                    args.Add("@endTime", endTime);
                }

                if (query.Length < 1) return;
                else
                {
                    query += "UPDATE \"Shifts\" SET \"EntryTime\" = CURRENT_TIMESTAMP WHERE \"Id\" = @shiftId;";
                }

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
                throw new Exception("Sql-Fehler UpdateShift()\r\n" + ex.GetType() + "\r\n" + ex.Message);
            }
        }

        /// <summary>
        /// Ändert den Eintrag für eine gesperrte Nachricht
        /// </summary>
        /// <param name="msgId">Id der Nachricht</param>
        /// <param name="days">Tage, an denen die Nachricht geperrt werden soll (wie Wochenuhr)</param>
        /// <param name="startHour">Stunde, ab der die Weiterleitung blockiert wird</param>
        /// <param name="endHour">Stunde, bis zu der die Weiterleitung blockiert wird</param>
        public void UpdateBlockedMessage(int msgId, int days = 0, int startHour = -1, int endHour = -1)
        {
            try
            {
                string query = string.Empty;
                var args = new Dictionary<string, object>
                {
                    { "@msgId", msgId }
                };

                if (days > 0 && days < 10) //1=Mo,2=Di,...7=So,8=Mo-Fr,9=Mo-So
                {
                    query += "UPDATE \"BlockedMessages\" SET \"Days\" = @days WHERE \"Id\" = @msgId;";
                    args.Add("@days", days);
                }

                if (startHour >= 0)
                {
                    query += "UPDATE \"BlockedMessages\" SET \"StartHour\" = @startHour WHERE \"Id\" = @msgId;";
                    args.Add("@startHour", startHour);
                }

                if (endHour >= 0)
                {
                    query += "UPDATE \"BlockedMessages\" SET \"EndHour\" = @endHour WHERE \"Id\" = @msgId;";
                    args.Add("@endHour", endHour);
                }

                if (query.Length < 1) return;
                else
                {
                    query += "UPDATE \"BlockedMessages\" SET \"EntryTime\" = CURRENT_TIMESTAMP WHERE \"Id\" = @msgId;";
                }

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
                throw new Exception("Sql-Fehler UpdateBlockedMessage\r\n" + ex.GetType() + "\r\n" + ex.Message);
            }
        }

    }
}
