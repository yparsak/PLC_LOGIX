using Logix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace PLCAPI.Models
{
    public class PLC
    {
        private Controller controller;
        private string PLC_IPAddress, PLC_Path;
        private int PLC_Timeout, PLC_Delay;

        private const string TRUE    = "TRUE";
        private const string FALSE   = "FALSE";
        private string PLC_ERR_CON_LOST = "PLC Connection Lost";

        private const string DateTime_Format = "{1}/{2}/{0} {3}:{4}:{5} {6}";

        private const string B = "<b>";
        private const string EB = "</b>";
        private const string BR = "<br />";

        private const string TAG_TIMEDATA           = "PLC_TimeData";
        private const string TAG_DISCRETEOUTPUT     = "DiscreteOutputData";

        public PLC()
        {

            try
            {
                PLC_IPAddress = WebConfigurationManager.AppSettings["PLC_IPAddress"];
                PLC_Path = WebConfigurationManager.AppSettings["PLC_Path"];
                PLC_Timeout = int.Parse(WebConfigurationManager.AppSettings["PLC_Timeout"]);
                PLC_Delay = int.Parse(WebConfigurationManager.AppSettings["PLC_Delay"]);

                controller = new Controller();
                controller.IPAddress = PLC_IPAddress;
                controller.Path = PLC_Path;
                controller.Timeout = PLC_Timeout;
                controller.CPUType = Controller.CPU.LOGIX;

                if (controller.Connect() != ResultCode.E_SUCCESS)
                {
                    Reconnect();
                }

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        ~PLC()
        {
            if (isConnected())
            {
                Disconnect();
            }

        }

        public bool isConnected()
        {
            try
            {
                return controller.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        private void Disconnect()
        {
            controller.Disconnect();
        }

        private void Reconnect()
        {
            if (!isConnected())
            {
                int retry = 0;
                while (retry < PLC_Timeout)
                {
                    retry++;
                    if (controller.Connect() == ResultCode.E_SUCCESS) return;
                    System.Threading.Thread.Sleep(PLC_Delay);
                }
                throw new Exception(PLC_ERR_CON_LOST);
            }
        }

        public string ReadTag(string name, Tag.ATOMIC type)
        {
            Reconnect();
            if (isConnected())
            {
                Tag tag = new Tag(name);
                tag.DataType = type;
                controller.ReadTag(tag);
                if (tag.QualityCode == ResultCode.QUAL_GOOD)
                {
                    return tag.Value.ToString();
                }
                else
                {
                    throw new Exception("Unable to read tag with quality code: " + tag.QualityCode);
                }
            }
            else
            {
                throw new Exception(PLC_ERR_CON_LOST);
            }
        }

        public bool BitArrayGet(int value, int index) {
            //Get boolean value from a 'Double Integer as Boolean' (DINT).
            System.Collections.BitArray bitarray = new System.Collections.BitArray(new int[] { value });
            return bitarray.Get(index);
        }

        public string info() {
            Reconnect();
            string output = "";
            if (isConnected()) {

                try
                {
                    output = B +"Host Name"+EB+": "+ WebConfigurationManager.AppSettings["PLC_IPAddress"] + BR;
                }
                catch (Exception e)
                { }

                output += B+"Date Time"+ EB +": " + GetDateTime() + BR + BR;
                output += "PLC Healthy Status:" + Bool2str(GetDINT(TAG_DISCRETEOUTPUT, 0));
            }
            return output;
        }

        public string Bool2str(bool val) {
            if (val) return TRUE;
            return FALSE;
        }
        public bool GetDINT(string tagname, int index) {
            int x = Int32.Parse(ReadTag(tagname, Tag.ATOMIC.INT));
            bool value = BitArrayGet(x, index);
            return value;
        }

        public string GetDateTime()
        {
            if (isConnected())
            {
                string TimeStr = String.Format(DateTime_Format,
                  ReadTag(TAG_TIMEDATA + "[0]", Tag.ATOMIC.INT),
                  ReadTag(TAG_TIMEDATA + "[1]", Tag.ATOMIC.INT),
                  ReadTag(TAG_TIMEDATA + "[2]", Tag.ATOMIC.INT),
                  ReadTag(TAG_TIMEDATA + "[3]", Tag.ATOMIC.INT),
                  ReadTag(TAG_TIMEDATA + "[4]", Tag.ATOMIC.INT),
                  ReadTag(TAG_TIMEDATA + "[5]", Tag.ATOMIC.INT),
                  ReadTag(TAG_TIMEDATA + "[6]", Tag.ATOMIC.INT)
                  );
                return TimeStr;
            }
            else
            {
                throw new Exception(PLC_ERR_CON_LOST);
            }
        }
    }
}