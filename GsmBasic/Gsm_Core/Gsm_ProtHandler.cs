using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// #### Inhalt bei Dateinamen Gsm_Prot*.cs NICHT ändern #### //
namespace MelBox
{
    public partial class Gsm
    {

        //Receive data from port
        internal void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (Port == null) return;

            Thread.Sleep(100); //Liest nicht immer den vollständigen Bytesatz 
            string answer = ReadFromPort();
                        
            //if ((answer.Length == 0) || ((!answer.EndsWith("\r\n> ")) && (!answer.EndsWith("\r\nOK\r\n"))))
            if (answer.Contains("ERROR"))
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11021909, "Fehlerhaft Empfangen:\n\r" + answer));
            }

            //Send data to whom ever interested
            OnRaiseGsmRecEvent(new GsmEventArgs(11051044, answer));

            //Interpretiere das empfangene auf verwertbare Inhalte
            HandleGsmRecEvent(answer);
        }

        /// <summary>
        /// Der eigentliche Lesevorgang von Port
        /// </summary>
        /// <returns></returns>
        private string ReadFromPort()
        {
            try
            {
                int dataLength = Port.BytesToRead;
                byte[] data = new byte[dataLength];
                int nbrDataRead = Port.Read(data, 0, dataLength);
                if (nbrDataRead == 0)
                    return string.Empty;
                return Encoding.ASCII.GetString(data);
            }
            catch
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11061406, string.Format("Der Port {0} ist nicht bereit.", CurrentComPortName)));
                return string.Empty;
            }
        }

        internal void HandleGsmRecEvent(string input)
        {
            // Die hier aufgerufenen Methoden sind zu finden in Gsm_Interpret.cs 

            if (input.Contains("+CMGL:"))
            {
                ParseMessages(input);
            }

            if (input.Contains("+CMGS:"))
            {
                ParseSmsIdFromSendResponse(input);
            }

            if (input.Contains("+CDSI:"))
            {
                //Indicates that new SMS status report has been received +CDS: / +CDSI:
                //erwartete Antwort: +CDSI: <mem3>, <index>
                //Lese Id der SMS von Empfangsbestätigung 
                ParseStatusReport(input);
            }

            if (input.Contains("+CSQ:"))
            {
                ParseSignalQuality(input);
            }

            if (input.Contains("+CREG:"))
            {
                ParseIsSimRegiserd(input);
            }

        }
    }
}
