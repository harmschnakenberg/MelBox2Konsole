using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// #### Inhalt bei Dateinamen Gsm_Prot*.cs NICHT ändern #### //
namespace MelBox
{
	public partial class Gsm
	{
		/// <summary>
		/// Event 'System-Ereignis'
		/// </summary>
		public event EventHandler<GsmEventArgs> RaiseGsmSystemEvent;

		/// <summary>
		/// Trigger für das Event 'System-Ereignis'
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnRaiseGsmSystemEvent(GsmEventArgs e)
		{
			RaiseGsmSystemEvent?.Invoke(this, e);
		}

		/// <summary>
		/// Event 'string gesendet an COM'
		/// </summary>
		public event EventHandler<GsmEventArgs> RaiseGsmSentEvent;

		/// <summary>
		/// Triggert das Event 'string gesendet an COM'
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnRaiseGsmSentEvent(GsmEventArgs e)
		{
			RaiseGsmSentEvent?.Invoke(this, e);
		}

		/// <summary>
		/// Event 'string empfangen von COM'
		/// </summary>
		public event EventHandler<GsmEventArgs> RaiseGsmRecEvent;

		/// <summary>
		/// Triggert das Event 'string empfangen von COM'
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnRaiseGsmRecEvent(GsmEventArgs e)
		{
			RaiseGsmRecEvent?.Invoke(this, e);
		}

	}

	/// <summary>
	/// einfache Ereignisse verursacht durch das Modem 
	/// </summary>
	public class GsmEventArgs : EventArgs
	{
		public GsmEventArgs(uint id, string message)
		{
			Id = id;
			Message = message;
		}

		public uint Id { get; set; }
		public string Message { get; set; }
	}

	public class GsmStatusReportEventArgs : EventArgs
	{
		public GsmStatusReportEventArgs(ulong phone, string message, bool sendSuccess)
		{
			Phone = phone;
			Message = message;
			SendSuccess = sendSuccess;
		}

		public ulong Phone { get; set; }
		public string Message { get; set; }

		public bool SendSuccess { get; set; }
	}
}
