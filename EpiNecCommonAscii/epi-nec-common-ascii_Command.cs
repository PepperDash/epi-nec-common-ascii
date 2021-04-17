using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharpPro.DM;

namespace EpiNecCommonAscii
{
    /// <summary>
    /// 
    /// </summary>
    public class NecAsciiCommand
    {
        private string _command;
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public string Command
        {
            get { return _command ?? string.Empty; }
            private set
            {
                if (value == null) throw new ArgumentNullException("value");
                _command = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public NecAsciiCommand(string command)
        {
            _command = command;
        }
    }
}