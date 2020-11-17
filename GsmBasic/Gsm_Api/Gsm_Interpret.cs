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

			if (input.Contains("+CMGS:") || input.Contains("+CMSS:")) //Gesendete SMS
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
				ParseStatusReportIndicator(input);
				
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
			} else

			if (input.Contains("AT+CMGD="))
			{
				int index = input.IndexOf("AT+CMGD=");
				string x = input.Substring(index + 9, 3).Trim();

				OnRaiseGsmSystemEvent(new GsmEventArgs(11131313, "SMS " + x + " wurde gelöscht."));
			}

		}
		
		#endregion

		#region Antworten aus GSM interpretieren

		private void ParseUnsentMessages(string input)
		{
			if (input == null) return;
			try
			{
				Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",,\r\n(.+)");
				Match m = r.Match(input);

				if (m.Length == 0)
				{
					OnRaiseGsmSystemEvent(new GsmEventArgs(11051658, "Keine neuen SMS zu senden.")); 
					return;
				}

				while (m.Success)
				{
					ShortMessageArgs msg = new ShortMessageArgs
					{
						Index = m.Groups[1].Value,
						Status = m.Groups[2].Value,
						Sender = m.Groups[3].Value,
						//Alphabet = m.Groups[4].Value,
						//Sent = m.Groups[5].Value,
						Message = m.Groups[4].Value
					};

					if (msg.Status == "STO UNSENT")
					{
						//Hier SMS zwischenspeichern zur späteren Auswertung "Sendebestätigung erhalten"
						SetRetrySendSmsTimer(msg);

						//Neue Nachricht senden
						//OnRaiseGsmSystemEvent(new GsmEventArgs(11111749, "Sende SMS " + msg.Index + " ab.")); ;
						OnRaiseSmsSentEvent(msg);
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
				Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)");
				Match m = r.Match(input);

				if (m.Length == 0)
				{
					System.IO.File.AppendAllText("d:\\temp\\rec"+DateTime.Now.Ticks+".txt", r.ToString() + "\r\n" + input);
					OnRaiseGsmSystemEvent(new GsmEventArgs(11051201, "Es wurde keine neuen SMS empfangen."));
					return;
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
						//FRAGE: nur 'REC READ' (nächster Zyklus) oder immer (sofort) löschen?
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
				//+CMGL: < index > ,  < stat > ,  < fo > ,  < mr > , [ < ra > ], [ < tora > ],  < scts > ,  < dt > ,  < st >
				//[... ]
				//OK
				//<st> 0-31 erfolgreich versandt; 32-63 versucht weiter zu senden: 64-127 Sendeversuch abgebrochen

				//z.B.: +CMGL: 1,"REC READ",6,34,,,"20/11/06,16:08:45+04","20/11/06,16:08:50+04",0
				//Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",(\d+),(\d+),,,""(.+)"",""(.+)"",(\d+)\r\n");
				Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",(\d+),(\d+),,,""(.+)"",""(.+)"",(\d+)");
				Match m = r.Match(input);

				if (m.Length == 0)
				{
					System.IO.File.AppendAllText("d:\\temp\\status" + DateTime.Now.Ticks + ".txt", r.ToString() + "\r\n" + input);
					OnRaiseGsmSystemEvent(new GsmEventArgs(11091346, "Es wurde keine neuen StatusReport-SMS empfangen"));
					return;
				}

				while (m.Success)
				{
					ShortMessageArgs statusMsg = new ShortMessageArgs
					{
						Index = m.Groups[1].Value,
						Status = m.Groups[2].Value,						
						Sender = m.Groups[3].Value,
						Alphabet = m.Groups[4].Value,
						Sent = m.Groups[5].Value,
					};

					byte reportStatusIndicator = 128;					
					byte.TryParse(m.Groups[7].Value, out reportStatusIndicator);

					//Nur zum Test
					string statusString = string.Empty;
					if (reportStatusIndicator < 32) { statusString = "erfolgreich versendet"; }
					else if (reportStatusIndicator < 64) { statusString = "Sendeversuch läuft noch"; }
					else if (reportStatusIndicator < 128) { statusString = "Sendeversuch abgebrochen"; }
					else { statusString = "Status unbekannt"; }

					int.TryParse(statusMsg.Index, out int statusReportSmsId);

					if (statusReportSmsId > 0)
					{						
						//Status-SMS mit Sendebestätigung für SMS 
						//Console.WriteLine("StatusReport Id {0} für SMS {1} Status: {2}\r\n{3}", statusReportSmsId, statusMsg.Sender, statusString, statusMsg.Message);
						OnRaiseGsmSystemEvent(new GsmEventArgs(11072203, string.Format("Empfangsbestätigung für die SMS '{0}' erhalten.", statusMsg.Sender)));

						//Gehe durch alle offenen Nachrichten
                        foreach (ShortMessageArgs sms in SendRetrys.Keys)
                        {
							int.TryParse(sms.Index, out int smsIndex);
							
							//Sendebestätigung == offene SMS?
							if (smsIndex == int.Parse(statusMsg.Sender))
                            {
								
								//Melde Sendebestätigung
								ulong phone = ulong.Parse(sms.Sender.Trim().Trim('+'));
								string message = sms.Message;

								if (phone == 0 || message.Length == 0)
								{
									OnRaiseGsmSystemEvent(new GsmEventArgs(11170844, "Nachricht konnte nicht durch Statusreport identifiziert werden."));
								}
								else
								{
									Console.WriteLine("Statusreport für wartende nachricht erhalten.");
									OnRaiseSmsTimeoutEvent(new GsmStatusReportEventArgs(phone, message, reportStatusIndicator));
								}

								//BAUSTELLE !! Kann keinen Member des Loops löschen!?! Bisher keine Fehlermeldung
								SendRetrys.Remove(sms);
								//Lösche bestätigte SMS
								SmsDelete(smsIndex);
								//Lösche EMpfangsbestätigung für bestätigte SMS
								SmsDelete(statusReportSmsId);
							}
						}

						if (statusMsg.Status == "REC READ")
						{
							Console.WriteLine("Lösche alten StatusReport " + statusReportSmsId);
							SmsDelete(statusReportSmsId);
						}


					}
					else
                    {
						OnRaiseGsmSystemEvent(new GsmEventArgs(11121558, "Empfangsbestätigung konnte nicht entziffert werden für:\r\n." + m.Value));
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

		//private void ParseSmsIdFromSendResponse(string input)
		//{
		//	if (input == null) return;

		//	Regex r = new Regex(@"\+CMGS: (\d+)");
		//	Match m = r.Match(input);

		//	while (m.Success)
		//	{
		//		if (int.TryParse(m.Groups[1].Value, out int id))
		//		{
		//			//Setze Timer zum Überwachen der Empfangsbestätigung
		//			Console.WriteLine("Id " + id + " der gesendeten Nachricht:\r\n" + input);
		//			return;
		//		}

		//		m = m.NextMatch();
		//	}
		//}

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
