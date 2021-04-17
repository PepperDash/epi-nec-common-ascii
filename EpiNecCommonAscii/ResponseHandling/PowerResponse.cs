using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;

namespace EpiNecCommonAscii.ResponseHandling
{
    public class PowerResponse : Response
    {
        public PowerResponse(string response) : base(response)
        {
            ResponseVal = response;
        }

        public bool GetPowerState()
        {
            return ResponseVal.Contains("on");
        }
    }
}