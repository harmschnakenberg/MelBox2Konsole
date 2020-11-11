using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MelBox
{
	public partial class Gsm
	{

		#region Interpretationsweiche

		void InterpretGsmRecEvent(object sender, GsmEventArgs e)
		{
			string input = e.Message;

			// Die hier aufgerufenen Methoden sind zu finden in Gsm_Interpret.cs 

			if (input.Contains("+CMGL:")) //SMS im SIM-Speicher
			{
				ParseRecMessages(input);
				ParseStatusReport(input);
				ParseUnsentMessages(input);
			} else

			if (input.Contains("+CMGS:")) //Gesendete SMS
			{
				//nur informativ:
				//ParseSmsIdFromSendResponse(input);
			} else

			if (input.Contains("+CDSI:") || input.Contains("+CMTI:"))
			{
				//Meldung einer neu eingegangenen Nachricht von GSM-Modem

				//bei AT+CNMI= [ <mode> ][,  1 ][,  <bm> ][,  <ds> ][,  <bfr> ]
				//erwartete Antwort: +CMTI: <mem3>, <index>				
				//-noch keine Methode-

				//Neuen Statusreport empfangen:
				//bei AT+CNMI= [ <mode> ][,  <mt> ][,  <bm> ][,  2 ][,  <bfr> ]
				//erwartete Antwort: +CDSI: <mem3>, <index>
				//nur informativ;
				//ParseStatusReportIndicator(input);
				
				//SmsRead(); //muss verhinderen, dass zu schnell hintereinander aufgerufen wird!

			} else

			if (input.Contains("+CSQ:"))
			{
				ParseSignalQuality(input);
			} else

			if (input.Contains("+CREG:"))
			{
				ParseIsSimRegiserd(input);
			} else

			if (input.Contains("+CRING: VOICE"))
            {
				OnRaiseGsmSystemEvent(new GsmEventArgs(11091419, "Es geht ein Sprachanruf ein!"));
			}

		}

		#endregion

		#region Antworten aus GSM interpretieren

		private void ParseUnsentMessages(string input)
		{
			if (input == null) return;
			try
			{
				Regex r = new Regex(@"\+CMGL:\s(\d+),""(.+)"",""(.+)"",,\n(.+)");
				Match m = r.Match(input);

				if (m.Length == 0)
				{
					OnRaiseGsmSystemEvent(new GsmEventArgs(11051658, "Keine neuen SMS zu senden.")); ;
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

					if (msg.Status == "STO UNSENT")
					{
						//Hier SMS zwischenspeichern zur späteren Auswertung "Sendebestätigung erhalten"
						SetRetrySendSmsTimer(msg);

						//Neue Nachricht senden
						OnRaiseGsmSystemEvent(new GsmEventArgs(11111749, "Sende SMS " + msg.Index + " ab.")); ;
						SendATCommand("AT+CMSS=" + msg.Index);
					}
					else
					if (msg.Status == "STO SENT")
					{
						OnRaiseGsmSystemEvent(new GsmEventArgs(11111751, "Versendete SMS " + msg.Index + " bereit zum löschen.")); ;

						if (int.TryParse(msg.Index, out int index))
						{
							if (index > 0)
							//Nachricht aus Speicher löschen
							SmsDelete(index);
						}
					}

						m = m.NextMatch();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private void ParseRecMessages(string input)
		{
			if (input == null) return;
			try
			{
				Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
				Match m = r.Match(input);

				if (m.Length == 0)
				{
					OnRaiseGsmSystemEvent(new GsmEventArgs(11051201, "Es wurde keine neuen SMS empfangen."));;
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

					if (msg.Status == "REC UNREAD")
					{
						//neue SMS empfangen
						OnRaiseSmsRecievedEvent(msg);
					}
					else if (msg.Status == "REC READ")
					{
						SmsDelete(int.Parse(msg.Index));
					}
						m = m.NextMatch();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private void ParseStatusReport(string input)
        {
			if (input == null) return;
			try
			{
				//z.B.: +CMGL: 1,"REC READ",6,34,,,"20/11/06,16:08:45+04","20/11/06,16:08:50+04",0
				Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",(\d+),(\d+),,,""(.+)"",""(.+)"",(\d+)");
				
				Match m = r.Match(input);

				if (m.Length == 0)
				{
					OnRaiseGsmSystemEvent(new GsmEventArgs(11091346, "Es wurde keine neuen StatusReport-SMS empfangen"));
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
					};
					
					int.TryParse(msg.Alphabet, out int confirmedSmsId);

					if (confirmedSmsId > 0)
					{
						//Lese Empängernummer und SMS-Text der SMS mit confirmedSmsId 
						
						//Status-SMS mit Sendebestätigung für SMS mit der Id 'phone'
						Console.WriteLine("Empfangsbestätigung für Id " + confirmedSmsId + "\r\n" + input);
						OnRaiseGsmSystemEvent(new GsmEventArgs(11072203, string.Format("Empfangsbestätigung für die SMS '{0}' erhalten.", confirmedSmsId)));

                        foreach (ShortMessageArgs sms in SendRetrys.Keys)
                        {
							if (sms.Index == msg.Alphabet)
                            {
								//BAUSTELLE !! Kann keinen Member des Loops löschen!?!
								SendRetrys.Remove(sms);
                            }
                        }					 
					}

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
					//Setze Timer zum Überwachen der Empfangsbestätigung
					Console.WriteLine("Id " + id + " der gesendeten Nachricht:\r\n" + input);
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

		private void ParseStatusReportIndicator(string input)
		{
			string pattern = "\\+CDSI: \"\\w+\",(\\d+)";
			string strResp2 = System.Text.RegularExpressions.Regex.Match(input, pattern).Groups[1].Value;
			if (strResp2 == null || strResp2.Length < 1)
				return; // false;
			int.TryParse(strResp2, out int IdStatusReport);

			//Lese Id von gesendeter SMS aus EMfangsbestätigungs-SMS
			//SendATCommand("AT+CMGR=" + IdStatusReport);



			OnRaiseGsmSystemEvent(new GsmEventArgs(11060744, "Neue Empfangsbestätigung erhalten mit Id " + IdStatusReport));
		}

        #endregion

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
