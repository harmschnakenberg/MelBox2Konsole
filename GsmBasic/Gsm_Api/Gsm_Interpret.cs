using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MelBox
{
	public partial class Gsm
	{
		#region Fields
		/// <summary>
		/// Ids von rausgegangenen SMS, für die noch keine EMpfangsbestätigung erhalten wurde.
		/// TODO: Ids die länger in dieser Liste bleiben erneut senden.
		/// </summary>
		private static readonly List<int> SentSmsNotConfirmed = new List<int>();
		
		#endregion


		#region Events mit Interpretationsanteil

		/// <summary>
		/// Event SMS empfangen
		/// </summary>
		public event EventHandler<ShortMessageArgs> RaiseSmsRecievedEvent;

		/// <summary>
		/// Trigger für das Event SMS empfangen
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnRaiseSmsRecievedEvent(ShortMessageArgs e)
		{
			RaiseSmsRecievedEvent?.Invoke(this, e);
		}

		/// <summary>
		/// Event 'Signalqualität'
		/// </summary>
		public event EventHandler<GsmEventArgs> RaiseGsmQualityEvent;

		/// <summary>
		/// Trigger für das Event 'Signalqualität Mobilfunknetz ermittelt'
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnRaiseGsmQualityEvent(GsmEventArgs e)
		{
			RaiseGsmQualityEvent?.Invoke(this, e);
		}

		#endregion

		private void ParseMessages(string input)
		{
			if (input == null) return;
			try
			{
				Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
				Match m = r.Match(input);

				if (m.Length == 0)
				{
					OnRaiseGsmSystemEvent(new GsmEventArgs(11051201, "Es wurde keine SMS empfangen."));
				}

				while (m.Success)
				{
					ShortMessageArgs msg = new ShortMessageArgs
					{
						Index = m.Groups[1].Value,
						Status = m.Groups[2].Value,
						Sender = m.Groups[3].Value,
						Alphabet = m.Groups[4].Value,
						Sent = m.Groups[5].Value,
						Message = m.Groups[6].Value
					};

					OnRaiseSmsRecievedEvent(msg);
					m = m.NextMatch();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private void ParseSignalQuality(string input)
		{
			string pattern = @"\+CSQ: \d+,";
			string strResp2 = System.Text.RegularExpressions.Regex.Match(input, pattern).Groups[0].Value;
			if (strResp2 == null)
				return;

			int.TryParse(strResp2.Substring(6, 2), out int sig_qual);

			if (sig_qual > 32) sig_qual = 0;

			sig_qual *= 100 / 31;

			OnRaiseGsmQualityEvent(new GsmEventArgs(11051212, sig_qual.ToString()));
		}

		private void ParseSmsIdFromSendResponse(string input)
		{
			if (input == null) return;

			Regex r = new Regex(@"\+CMGS: (\d+)");
			Match m = r.Match(input);

			while (m.Success)
			{
				if (int.TryParse(m.Groups[1].Value, out int id))
				{
					Console.WriteLine("Id " + id + " der gesendeten Nachricht:\r\n" + input);
					SentSmsNotConfirmed.Add(id);
					return;
				}

				m = m.NextMatch();
			}
		}

		private void ParseIsSimRegiserd(string input)
		{

			string pattern = @"\+CREG: \d,\d";
			string strResp2 = System.Text.RegularExpressions.Regex.Match(input, pattern).Groups[0].Value;
			if (strResp2 == null)
				return; // false;

			//int.TryParse(strResp2.Substring(7, 1), out int RegisterStatus);
			//int.TryParse(strResp2.Substring(9, 1), out int AccessStatus);

			bool isSimRegistered = (strResp2 != "+CREG: 0,0");

			OnRaiseGsmSystemEvent(new GsmEventArgs(11060744, "Mobilfunktnetz: " + (isSimRegistered ? "registriert" : "kein Empfang")));

		}

		private void ParseStatusReport(string input)
		{
			string pattern = "\\+CDSI: \"\\w+\",(\\d+)";
			string strResp2 = System.Text.RegularExpressions.Regex.Match(input, pattern).Groups[1].Value;
			if (strResp2 == null || strResp2.Length < 1)
				return; // false;
			int.TryParse(strResp2, out int SentSuccess_SmsId);

			if (SentSmsNotConfirmed.Contains(SentSuccess_SmsId))
			{
				//Id aus Out-Liste "SentSmsNotConfirmed" löschen:
				SentSmsNotConfirmed.Remove(SentSuccess_SmsId);
			}

			OnRaiseGsmSystemEvent(new GsmEventArgs(11060744, "PROVISORISCH: Empfangsbestätigung für SMS mit Id " + SentSuccess_SmsId));
		}


	}

	/// <summary>
	/// "Tranbsportverpackung" einer SMS
	/// Absichtlich ist alles als string im "Rohformat" gelassen.
	/// Interpretation an anderer Stelle.
	/// </summary>
	public class ShortMessageArgs : EventArgs
	{

		#region Private Variables
		private string index;
		private string status;
		private string sender;
		private string alphabet;
		private string sent;
		private string message;
		#endregion

		#region Public Properties
		public string Index
		{
			get { return index; }
			set { index = value; }
		}
		public string Status
		{
			get { return status; }
			set { status = value; }
		}
		public string Sender
		{
			get { return sender; }
			set { sender = value; }
		}
		public string Alphabet
		{
			get { return alphabet; }
			set { alphabet = value; }
		}
		public string Sent
		{
			get { return sent; }
			set { sent = value; }
		}
		public string Message
		{
			get { return message; }
			set { message = value; }
		}
		#endregion
	}

}
