using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MelBox
{
    //GsmA = GSM Advanced
    public partial class GsmA
    {
        //Basic Class for GSM Connection & Communication
        public static GsmB gsm;

        public GsmA()
        {
            gsm = new GsmB();

            //Textmode
            gsm.SendATCommand("AT+CMGF=1");
            Thread.Sleep(200);

            gsm.SendATCommand("AT+CPMS=\"SM\"");
            //SendATCommand("AT+CPMS=\"SM\",\"SM\",\"SM\"");
            Thread.Sleep(200);

            //Erzwinge, dass bei Fehlerhaftem senden "+CMS ERROR: <err>" ausgegeben wird
            gsm.SendATCommand("AT^SM20=0,0");
            Thread.Sleep(200);

            //SIM-Karte im Mobilfunknetz registriert?
            gsm.SendATCommand("AT+CREG?");

            //Signalqualität
            gsm.SendATCommand("AT+CSQ");

        }

    }
}
