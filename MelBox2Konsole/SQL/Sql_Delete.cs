using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace MelBox
{
    public partial class MelSql
    {

        /// <summary>
        /// Entfernt eine Nachricht aus den blockierten Einträgen
        /// </summary>
        /// <param name="msgId">ID des Nachrichtentextes aus Tabelle MessageContent</param>
        public void DeleteBlockedMessage(int msgId)
        {
            try
            {
                //Nur neuen Eintrag erzeugen, wenn msgId noch nicht vorhanden ist.
                const string query = "DELETE FROM \"BlockedMessages\" WHERE \"Id\" = @msgId";

                var args = new Dictionary<string, object>
                {
                    {"@msgId", msgId}
                };

                using (SQLiteConnection con = new SQLiteConnection(Datasource))
                {
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
                OnRaiseSqlErrorEvent("DeleteBlockedMessage()", ex);
                throw new Exception("Sql-Fehler DeleteBlockedMessage()");
            }
        }

    }
}
