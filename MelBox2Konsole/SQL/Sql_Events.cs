using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelBox
{
    public partial class MelSql
    {
        /// <summary>
		/// Event 'System-Ereignis'
		/// </summary>
		public event EventHandler<SqlErrorEventArgs> RaiseSqlErrorEvent;

        /// <summary>
        /// Trigger für das Event 'System-Ereignis'
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRaiseSqlErrorEvent(string Source, Exception ex)
        {
            SqlErrorEventArgs e = new SqlErrorEventArgs(Source, ex.GetType().Name, ex.Message);
            RaiseSqlErrorEvent?.Invoke(this, e);
        }
    }

    public class SqlErrorEventArgs : EventArgs
    {
        public SqlErrorEventArgs(string source, string type, string message)
        {
            Source = source;
            Type = type;
            Message = message;
        }

        public string Source { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
