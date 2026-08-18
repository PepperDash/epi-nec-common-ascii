using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace EpiNecCommonAscii.ResponseHandling
{
    public class Response
    {
        protected string ResponseVal;

        public Response(string response)
        {
            ResponseVal = response;
        }
    }
}