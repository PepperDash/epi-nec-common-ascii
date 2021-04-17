using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using EpiNecCommonAscii.ResponseHandling;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Routing;
using PepperDash.Essentials.Core.Bridges;

namespace EpiNecCommonAscii  
{

	/// <summary>
	/// Plugin device
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.
	/// </remarks>
	/// <example>
	/// "EssentialsPluginDeviceTemplate" renamed to "SamsungMdcDevice"
	/// </example>
	public class NecCommonAsciiDevice : TwoWayDisplayBase, IBridgeAdvanced
	{
		/// <summary>
		/// It is often desirable to store the config
		/// </summary>
		private NecCommonAsciiDeviceConfigObject _config;

		#region IBasicCommunication Properties and Constructor.

		// TODO [ ] Add, modify, remove properties and fields as needed for the plugin being developed
		private readonly IBasicCommunication _comms;
		private readonly GenericCommunicationMonitor _commsMonitor;
		
		// TODO [ ] Delete the properties below if using a HEX/byte based API		
		// _comms gather is commonly used for ASCII based API's with deelimiters
		private readonly CommunicationGather _commsGather;

		/// <summary>
		/// Set this value to that of the delimiter used by the API (if applicable)
		/// </summary>
		private const string CommsDelimiter = "\r";

		/// <summary>
		/// Reports connect feedback through the bridge
		/// </summary>
		public BoolFeedback ConnectFeedback { get; private set; }

		/// <summary>
		/// Reports online feedback through the bridge
		/// </summary>
		public BoolFeedback OnlineFeedback { get; private set; }

		/// <summary>
		/// Reports socket status feedback through the bridge
		/// </summary>
		public IntFeedback SocketStatusFeedback { get; private set; }

		/// <summary>
		/// Reports monitor status feedback through the bridge
		/// Typically used for Fusion status reporting and system status LED's
		/// </summary>
		public IntFeedback MonitorStatusFeedback { get; private set; }

        public Dictionary<string,string> InputList { get; private set; }

	    private bool _isWarming;
	    public bool IsWarming
	    {
	        get { return _isWarming; }
	        private set
	        {
	            _isWarming = value;
	            IsWarmingUpFeedback.FireUpdate();
	        }
	    }

	    private bool _isCooling;

	    public bool IsCooling
	    {
	        get { return _isCooling; }
	        private set
	        {
	            _isCooling = value;
	            IsCoolingDownFeedback.FireUpdate();
	        }
	    }

	    private bool _powerIsOn;

	    public bool PowerIsOn
	    {
            get { return _powerIsOn; }
            private set
            {
                _powerIsOn = value;
                PowerIsOnFeedback.FireUpdate();
            }
	    }

	    private readonly uint _coolingTimeMs;
	    private readonly uint _warmingTimeMs;

	    private int _inputNumber;

	    public int InputNumber
	    {
	        get { return _inputNumber; }
	        set
	        {
	            ExecuteSwitch(InputPorts.ElementAt(value).Selector);
	        }
	    }

		/// <summary>
		/// Plugin device constructor
		/// </summary>
		/// <param name="key">device key</param>
		/// <param name="name">device name</param>
		/// <param name="config">device configuration object</param>
		/// <param name="comms">device communication as IBasicCommunication</param>
		/// <see cref="PepperDash.Core.IBasicCommunication"/>
		/// <seealso cref="Crestron.SimplSharp.CrestronSockets.SocketStatus"/>
		public NecCommonAsciiDevice(string key, string name, NecCommonAsciiDeviceConfigObject config, IBasicCommunication comms)
			: base(key, name)
		{
			Debug.Console(0, this, "Constructing new {0} instance", name);

			// TODO [ ] Update the constructor as needed for the plugin device being developed

		    InputList = new Dictionary<string, string>()
		    {
               {RoutingPortNames.HdmiIn,    "hdmi1"},
               {RoutingPortNames.HdmiIn1,   "hdmi1"},
               {RoutingPortNames.HdmiIn2,   "hdmi2"},
               {RoutingPortNames.RgbIn,    "computer"},
               {RoutingPortNames.RgbIn1,   "computer"}      
		    };

			_config = config;

			ConnectFeedback = new BoolFeedback(() => _comms.IsConnected);
			OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
			MonitorStatusFeedback = new IntFeedback(() => (int)_commsMonitor.Status);			

			_comms = comms;
			_commsMonitor = new GenericCommunicationMonitor(this, _comms, _config.PollTimeMs, _config.WarningTimeoutMs, _config.ErrorTimeoutMs, Poll);

			var socket = _comms as ISocketStatus;
			if (socket != null)
			{
				// device comms is IP **ELSE** device comms is RS232
				socket.ConnectionChange += socket_ConnectionChange;
				SocketStatusFeedback = new IntFeedback(() => (int)socket.ClientStatus);
			
            }

		    _warmingTimeMs = _config.WarmingTimeMs;
		    _coolingTimeMs = _config.CoolingTimeMs;

			#region Communication data event handlers.  Comment out any that don't apply to the API type			

			// _comms gather is commonly used for ASCII based API's that have a defined delimiter
			_commsGather = new CommunicationGather(_comms, CommsDelimiter);			
			// Event fires when the defined delimter is found
			_commsGather.LineReceived += Handle_LineRecieved;
	
			#endregion Communication data event handlers.  Comment out any that don't apply to the API type

			Debug.Console(0, this, "Constructing new {0} instance complete", name);
			Debug.Console(0, new string('*', 80));
			Debug.Console(0, new string('*', 80));
		}

		/// <summary>
		/// Use the custom activiate to connect the device and start the comms monitor.
		/// This method will be called when the device is built.
		/// </summary>
		/// <returns></returns>
		public override bool CustomActivate()
		{
			// Essentials will handle the connect method to the device                       
			_comms.Connect();
			// Essentialss will handle starting the comms monitor
			_commsMonitor.Start();

			return base.CustomActivate();
		}

		private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
		{
			if (ConnectFeedback != null)
				ConnectFeedback.FireUpdate();

			if (SocketStatusFeedback != null)
				SocketStatusFeedback.FireUpdate();
		}

		// TODO [ ] Delete the properties below if using a HEX/byte based API
		// commonly used with ASCII based API's with a defined delimiter				
		private void Handle_LineRecieved(object sender, GenericCommMethodReceiveTextArgs args)
		{
			// TODO [ ] Implement method 
            var SplitChar = new char[] {' '};
		    var DataReceived = args.Text;
		    var Parts = DataReceived.Split(SplitChar);

		    if (Parts[0].ToLower().Contains("power"))
		    {
		        var HandleResponse = new PowerResponse(new NecAsciiCommand(Parts.ToString());
		        if (Parts[1].ToLower().Contains("on"))
		        {
		            IsWarming = false;
		            IsCooling = false;
                    PowerIsOn = true;
		        }
		        if (Parts[1].ToLower().Contains("off"))
		        {
		            IsWarming = false;
		            IsCooling = false;
		            PowerIsOn = true;
		        }
                if (Parts[1].ToLower().Contains("warming"))
                {
                    IsWarming = true;
                    IsCooling = false;
                    PowerIsOn = false;
                }
                if (Parts[1].ToLower().Contains("cooling"))
                {
                    IsWarming = false;
                    IsCooling = true;
                    PowerIsOn = false;
                }

		        return;
		    }
            if(Parts[0])

            

		    Parts = DataReceived.Split(SplitChar);


		}


		// TODO [ ] Delete the properties below if using a HEX/byte based API
		/// <summary>
		/// Sends text to the device plugin comms
		/// </summary>
		/// <remarks>
		/// Can be used to test commands with the device plugin using the DEVPROPS and DEVJSON console commands
		/// </remarks>
		/// <param name="text">Command to be sent</param>		
		public void SendText(string text)
		{
			if (string.IsNullOrEmpty(text)) return;

			_comms.SendText(string.Format("{0}{1}", text, CommsDelimiter));
		}

		/// <summary>
		/// Polls the device
		/// </summary>
		/// <remarks>
		/// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed
		/// </remarks>
		public void Poll()
		{
			// TODO [ ] Update Poll method as needed for the plugin being developed
			// Example: SendText("getStatus");
			
            SendText("status");
		}

		#endregion IBasicCommunication Properties and Constructor.


	    private void AddRoutingInputPort(RoutingInputPort port, string fbMatch)
	    {
	        port.FeedbackMatchObject = fbMatch;
            InputPorts.Add(port);
	    }

	    private void InitializeRoutingInputPorts()
	    {
	        AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn1, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, new Action(() => InputSelect(new NecAsciiCommand(RoutingPortNames.HdmiIn1))),this), InputList[RoutingPortNames.HdmiIn1]);
	        AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.RgbIn1, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Rgb, new Action(() => InputSelect(new NecAsciiCommand(RoutingPortNames.RgbIn1))), this), InputList[RoutingPortNames.RgbIn1]);
	    }


	    /// <summary>
	    /// 
	    /// </summary>
	    /// <param name="command"></param>
	    public void InputSelect(NecAsciiCommand command)
	    {
	        if (command == null) return;

	        var commandString = command.Command;

	        if (commandString == string.Empty ||!InputList.ContainsKey(commandString)) return;

            SendText(string.Format("input {0}",InputList[commandString]));
	    }

	    #region TwoWayDisplayBase Implementation


        public override void PowerOn()
        {
            if (PowerIsOn || IsWarming || IsCooling) return;

            IsWarming = true;

	        SendText("power on");
            WarmupTimer = new CTimer(o =>
                                             {
                                                 IsWarming = false;
                                                 PowerIsOn = true;
                                             }, _warmingTimeMs);
        }

	    public override void PowerOff()
	    {
	        if (!PowerIsOn || IsWarming || IsCooling) return;

	        IsCooling = true;

            SendText("power off");

	        PowerIsOn = false;
            //Set Input to clear
            CooldownTimer = new CTimer(o =>
                                           {
                                               IsCooling = false;
                                           }, _coolingTimeMs);
	    }

	    public override void PowerToggle()
	    {
	        if (PowerIsOn)
	        {
	            PowerOff();
	            return;
	        }
            PowerOn();
	    }

	    public override void ExecuteSwitch(object selector)
	    {
	        if (!(selector is Action))
	            return;

	        if (!PowerIsOnFeedback.BoolValue)
	        {
	            EventHandler<FeedbackEventArgs> handler = null;
	            handler = (o, a) =>
	                          {
	                              if (!PowerIsOnFeedback.BoolValue) return;
	                              PowerIsOnFeedback.OutputChange -= handler;
	                              var action = selector as Action;
	                              if (action != null) action();
	                          };
	            PowerIsOnFeedback.OutputChange += handler;
                PowerOn();
	            return;
	        }

	        PowerOn();
	    }

	    protected override Func<bool> PowerIsOnFeedbackFunc
	    {
	        get { return () => PowerIsOn; }
	    }

	    protected override Func<bool> IsCoolingDownFeedbackFunc
	    {
	        get { return () => IsCooling; }
	    }

	    protected override Func<bool> IsWarmingUpFeedbackFunc
	    {
	        get { return () => IsWarming; }
	    }

	    protected override Func<string> CurrentInputFeedbackFunc
	    {
	        get { throw new NotImplementedException(); }
        }

        #endregion TwoWayDisplayBase Implementation

        #region IBridgeAdvanced Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trilist"></param>
        /// <param name="joinStart"></param>
        /// <param name="joinMapKey"></param>
        /// <param name="bridge"></param>
        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            LinkDisplayToApi(this, trilist, joinStart, joinMapKey, new EiscApiAdvanced(bridge));
        }

        #endregion
    }
}

