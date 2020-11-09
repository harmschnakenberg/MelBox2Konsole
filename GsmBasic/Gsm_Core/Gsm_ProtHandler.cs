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
            //HandleGsmRecEvent(answer); //wird jetzt über Ereignis RaiseGsmRecEvent getriggert
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
            catch (TimeoutException ex_time)
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11061406, string.Format("Der Port {0} konnte nicht erreicht werden. Timeout. \r\n{1}\r\n{2}", CurrentComPortName, ex_time.GetType(), ex_time.Message)));
                return string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.GetType().Name + "\r\n" + ex.Message);
            }
        }

    }
}
