using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace EpiNecCommonAscii.ResponseHandling
{
    public class AVMuteResponse
    {
        
        public AVMuteResponse(NecAsciiCommand response)
        {
            Response = response;
        }


    }
}