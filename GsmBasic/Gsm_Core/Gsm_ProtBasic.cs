
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

// #### Inhalt bei Dateinamen Gsm_Prot*.cs NICHT ändern #### //
namespace MelBox
{
    public partial class Gsm
    {
        #region Fields
        public SerialPort Port;
        const int maxConnectTrys = 5;
        int currentConnectTrys = 0;
        #endregion

        #region Properties
        public string CurrentComPortName { get; set; } = Properties.Settings.Default.ComPort;
        #endregion

        #region Methods

        /// <summary>
        /// Versuche Verbindung herzustellen und GSM-Modem einzurichten
        /// </summary>
        public Gsm()
        {
            int trys = 0;
            while (trys < 3 && (Port == null || !Port.IsOpen))
            {
                //Öffne COM-PORT
                if (Port == null)
                    OnRaiseGsmSystemEvent(new GsmEventArgs(11061347, string.Format("{0}/3 Verbindungsversuch an Port {1}", ++trys, CurrentComPortName)));
                else
                {                    
                    OnRaiseGsmSystemEvent(new GsmEventArgs(11011901, string.Format("{0}/3 Öffne Port {1}", ++trys, Port.PortName)));
                }

                Console.WriteLine("Verbindungsversuch " + trys);
                ConnectPort();
                
                if (Port == null || !Port.IsOpen)
                    Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Verbindet den COM-Port
        /// </summary>
        private void ConnectPort()
        {           
            #region richtigen COM-Port ermitteln
            List<string> AvailableComPorts = System.IO.Ports.SerialPort.GetPortNames().ToList();

            if (AvailableComPorts.Count < 1)
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11011512, "Es sind keine COM-Ports vorhanden"));
                return;
            }

            if (!AvailableComPorts.Contains(CurrentComPortName))
            {
                CurrentComPortName = AvailableComPorts.LastOrDefault();
            }
            #endregion

            #region Wenn Port bereits vebunden ist, trennen
            if (Port != null && Port.IsOpen)                
            {
                ClosePort();
            }
            #endregion

            #region Verbinde ComPort

            OnRaiseGsmSystemEvent(new GsmEventArgs(11051108, string.Format("Öffne Port {0}...", CurrentComPortName)));

            SerialPort port = new SerialPort();

            try
            {
                while (port == null || !port.IsOpen)
                {
                    port.PortName = CurrentComPortName;                     //COM1
                    port.BaudRate = Properties.Settings.Default.BaudRate;   //9600
                    port.DataBits = 8;                                      //8
                    port.StopBits = StopBits.One;                           //1
                    port.Parity = Parity.None;                              //None
                    port.ReadTimeout = 300;                                 //300
                    port.WriteTimeout = 300;                                //300
                    port.Encoding = Encoding.GetEncoding("iso-8859-1");
                    port.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);
                    port.Open();
                    port.DtrEnable = true;
                    port.RtsEnable = true;

                    if (currentConnectTrys++ > maxConnectTrys)
                    {
                        OnRaiseGsmSystemEvent(new GsmEventArgs(11061519, "Maximale Anzahl Verbindungsversuche zu " + CurrentComPortName + " überschritten."));
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Verbindungsversuch " + currentConnectTrys + " von " + maxConnectTrys);
                        OnRaiseGsmSystemEvent(new GsmEventArgs(11061554, "Verbindungsversuch " + currentConnectTrys + " von " + maxConnectTrys));
                        Thread.Sleep(2000);
                    }
                }

                currentConnectTrys = 0;
            }
            catch (ArgumentException ex_arg)
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11011514, string.Format("COM-Port {0} konnte nicht verbunden werden. \r\n{1}\r\n{2}", CurrentComPortName, ex_arg.GetType(), ex_arg.Message)));
            }
            catch (UnauthorizedAccessException ex_unaut)
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11011514, string.Format("Der Zugriff auf COM-Port {0} wurde verweigert. \r\n{1}\r\n{2}", CurrentComPortName, ex_unaut.GetType(), ex_unaut.Message)));
            }
            catch (System.IO.IOException ex_io)
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11011514, string.Format("Das Modem konnte nicht an COM-Port {0} erreicht werden. \r\n{1}\r\n{2}", CurrentComPortName, ex_io.GetType(), ex_io.Message)));
            }

            Port = port;
            #endregion
        }

        //Close Port
        public void ClosePort()
        {
            if (Port == null) return;

            OnRaiseGsmSystemEvent(new GsmEventArgs(11011917, "Port " + Port.PortName + " wird geschlossen.\r\n"));
            try
            {
                Port.Close();
                Port.DataReceived -= new SerialDataReceivedEventHandler(Port_DataReceived);
                Port.Dispose();
                Port = null;
                System.Threading.Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SendATCommand(string command)
        {
            if (Port == null || !Port.IsOpen )
            {
                ConnectPort();
                Thread.Sleep(2000);
            }

            try
            {
                Thread.Sleep(100);
                Port.Write(command + "\r");
                OnRaiseGsmSentEvent(new GsmEventArgs(11051045, command));
                Thread.Sleep(100);
            }
            catch (System.IO.IOException ex_io)
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11021909, ex_io.Message));                
            }
            catch (InvalidOperationException ex_inval)
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11061455, ex_inval.Message));
            }
            catch (UnauthorizedAccessException ex_unaut)
            {
                OnRaiseGsmSystemEvent(new GsmEventArgs(11021910, ex_unaut.Message));
            }

        }

        #endregion
    }

}
