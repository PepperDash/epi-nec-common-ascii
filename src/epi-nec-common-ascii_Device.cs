using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using EpiNecCommonAscii.ResponseHandling;
using PepperDash.Core;
using PepperDash.Essentials.Devices.Common.Displays;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Routing;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Core.Logging;

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
    public class NecCommonAsciiDevice : TwoWayDisplayBase, IBridgeAdvanced, ICommunicationMonitor, IHasInputs<string>, IWarmingCooling
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        private NecCommonAsciiDeviceConfigObject _config;

        #region IBasicCommunication Properties and Constructor.

        // TODO [ ] Add, modify, remove properties and fields as needed for the plugin being developed
        private readonly IBasicCommunication Comms;
		public StatusMonitorBase CommunicationMonitor { get; private set; }


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

		#endregion IBasicCommunication Properties and Constructor.

        public IntFeedback LampHoursFeedback { get; private set; }
        public StringFeedback LampHoursStringFeedback { get; private set; }

        public Dictionary<string, string> InputList { get; private set; }
		private readonly NecAsciiInputs _inputs;

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

        private int _lampHours;
        public int LampHours
        {
            get { return _lampHours; }
            set
            {
				
                _lampHours = value;
				
                LampHoursFeedback.FireUpdate();
                LampHoursStringFeedback.FireUpdate();     
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
                _inputNumber = value;
				
				UpdateBooleanFeedback();
				InputNumberFeedback.FireUpdate();
				CurrentInputFeedback.FireUpdate();
				Debug.Console(2, this, "newInputNumber {0}", _inputNumber);
            }
        }
		public List<BoolFeedback> InputFeedback;
		public IntFeedback InputNumberFeedback;

		public BoolFeedback VideoMuteIsOnFeedBack;
		private bool _VideoMuteState;
		public bool VideoMuteState
		{
			get { return _VideoMuteState; }
            private set
            {
                _VideoMuteState = value;
                VideoMuteIsOnFeedBack.FireUpdate();
            }
		}

		public BoolFeedback FreezeImageIsOnFeedBack;
		private bool _FreezeImageState;
		public bool FreezeImageState
		{
			get { return _FreezeImageState; }
			private set
			{
				_FreezeImageState = value;
				FreezeImageIsOnFeedBack.FireUpdate();
			}
		}
		private int PollState = 0;
	
		public NecCommonAsciiDevice(string key, string name, NecCommonAsciiDeviceConfigObject config, IBasicCommunication comms)
			: base(key, name)
		{
			Debug.Console(0, this, "Constructing new {0} instance", name);

			_inputs = new NecAsciiInputs();
			//When the CurrentItemChanged event is invoked, it calls the InputSelect method with the CurrentItem string
			_inputs.CurrentItemChanged += (sender, args) => InputSelect(new NecAsciiCommand(_inputs.CurrentItem));

			// TODO [ ] Update the constructor as needed for the plugin device being developed

		    InputList = new Dictionary<string, string>()
		    {
               {RoutingPortNames.HdmiIn,    "hdmi1"},
               {RoutingPortNames.HdmiIn1,   "hdmi1"},
               {RoutingPortNames.HdmiIn2,   "hdmi2"},
               {RoutingPortNames.RgbIn,    "computer"},
               {RoutingPortNames.RgbIn1,   "computer"}      

		    };
			InitializeRoutingInputPorts();
			_config = config;

			ConnectFeedback = new BoolFeedback(() => Comms.IsConnected);
			OnlineFeedback = new BoolFeedback(() => CommunicationMonitor.IsOnline);
			MonitorStatusFeedback = new IntFeedback(() => (int)CommunicationMonitor.Status);	
		    LampHoursFeedback = new IntFeedback(() => 
				{
				return LampHours; 	
				});
			LampHoursStringFeedback = new StringFeedback(() => LampHours.ToString());
			VideoMuteIsOnFeedBack = new BoolFeedback(() => _VideoMuteState);
			FreezeImageIsOnFeedBack = new BoolFeedback(() => _FreezeImageState);
			

			Comms = comms;

			CommunicationMonitor = new GenericCommunicationMonitor(this, Comms, 5000, 120000, 300000, Poll);

			var socket = Comms as ISocketStatus;
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
			_commsGather = new CommunicationGather(Comms, CommsDelimiter);			
			// Event fires when the defined delimter is found
			_commsGather.LineReceived += Handle_LineRecieved;
			
	
			#endregion Communication data event handlers.  Comment out any that don't apply to the API type

			PowerIsOnFeedback.OutputChange += PowerIsOnFeedback_OutputChange;

			IsWarmingUpFeedback.OutputChange += IsWarmingUpFeedback_OutputChange;

			IsCoolingDownFeedback.OutputChange += IsCoolingDownFeedback_OutputChange;

			Debug.Console(0, this, "Constructing new {0} instance complete", name);
			Debug.Console(0, new string('*', 80));
			Debug.Console(0, new string('*', 80));
		}

      	private void PowerIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            this.LogInformation("PowerIsOnFeedback changed to {0}", PowerIsOnFeedback.BoolValue);
			IsWarming = false;
            IsCooling = false;
        }
        private void IsWarmingUpFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            this.LogInformation("IsWarmingUpFeedback changed to {0}", IsWarmingUpFeedback.BoolValue);
        }

		private void IsCoolingDownFeedback_OutputChange(object sender, FeedbackEventArgs e)
		{
			this.LogInformation("IsCoolingDownFeedback changed to {0}", IsCoolingDownFeedback.BoolValue);
		}


        /// <summary>
        /// Use the custom activiate to connect the device and start the comms monitor.
        /// This method will be called when the device is built.
        /// </summary>
        /// <returns></returns>
        public override bool CustomActivate()
		{
			// Essentials will handle the connect method to the device                       
			Comms.Connect();
			// Essentialss will handle starting the comms monitor
			CommunicationMonitor.Start();

			return base.CustomActivate();
		}

		private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
		{
			if (ConnectFeedback != null)
				ConnectFeedback.FireUpdate();

			if (SocketStatusFeedback != null)
				SocketStatusFeedback.FireUpdate();

			if (OnlineFeedback != null)
			{
				OnlineFeedback.FireUpdate(); 
			}
		}
		
		private void Handle_LineRecieved(object sender, GenericCommMethodReceiveTextArgs args)
		{
			// TODO [ ] Implement method 
			//Debug.Console(2, this, "Handle_LineRecieved {0} ", args.Text);
			
			if (args.Text.Contains(" "))
			{
				var dataReceived = args.Text;
				var parts = dataReceived.Split(' ');

				var responseType = parts[0].ToLower();
				var responseValue = parts[1].ToLower();
				Debug.Console(2, this, "responseType ={0} responseValue ={1}", responseType, responseValue);
				if (responseType.Contains("power")) UpdatePower(responseValue);
				else if (responseType.Contains("input")) UpdateInput(responseValue);
				else if (responseType.Contains("status")) UpdateStatus(responseValue);
				else if (responseType.Contains("shutter"))
				{
					if (responseValue.Contains("open"))
					{
						VideoMuteState = false;
					}
					else if (responseValue.Contains("close"))
					{
						VideoMuteState = true;
					}
				}
				else if (responseType.Contains("freeze"))
				{
					if (responseValue.Contains("off"))
					{
						FreezeImageState = false;
					}
					else if (responseValue.Contains("on"))
					{
						FreezeImageState = true;
					}
				}
				else if (responseType.Contains("usage"))
				{
					LampHours = Int16.Parse(parts[3].Trim());
				}
			}
		}

        private void UpdateUsage(string response)
        {
            if (response.Contains("light hours"))
            {
                
            }
        }
		private void UpdateBooleanFeedback()
		{
			try
			{
				foreach (var item in InputFeedback)
				{
					item.FireUpdate();
				}
			}
			catch (Exception e)
			{
				Debug.Console(0, this, "Exception Here - {0}", e.Message);
			}
		}
        private void UpdateStatus(string response)
        {
            const char splitChar = ';';

            var statusMsg = response.Split(splitChar);

            UpdatePower(statusMsg[0].ToLower());
        }

	    private void UpdatePower(string response)
	    {
            if (response.Contains("on"))
            {
                //IsWarming = false;
                //IsCooling = false;
                PowerIsOn = true;
                return;
            }
            if (response.Contains("off"))
            {
                //IsWarming = false;
                //IsCooling = false;
				PowerIsOn = false;
                return;
            }
            if (response.Contains("warming"))
            {
                IsCooling = false;
                PowerIsOn = true;
                IsWarming = true;
                return;
            }
			if (response.Contains("busy"))
            {
                IsCooling = false;
                PowerIsOn = true;
                IsWarming = true;
                return;
            }
            if (response.Contains("cooling"))
            {
                IsWarming = false;
                PowerIsOn = true;
                IsCooling = true;
                return;
            }

	        if (response.Contains("running"))
	        {
                IsWarming = false;
                IsCooling = false;
                PowerIsOn = true;
                return;
	        }

	        if (response.Contains("standby"))
	        {
                IsWarming = false;
                IsCooling = false;
                PowerIsOn = false;
                return;
	        }

            IsWarming = false;
            PowerIsOn = false;
            IsCooling = false;
	    }

	    private void UpdateInput(string response)
	    {
			Debug.Console(2, this, "UpdateInput {0}", response);
			InputNumber = GetInputNumberFromName(response);
			SyncSelectableInputs();
	    }
	
		public void SendText(string text)
		{
			if (string.IsNullOrEmpty(text)) return;

			Comms.SendText(string.Format("{0}{1}", text, CommsDelimiter));
			//Debug.Console(2, this, "SendText {0}", text);
		}

		/// <summary>
		/// Polls the device
		/// </summary>
		/// <remarks>
		/// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed
		/// </remarks>
		public void Poll()
		{
			switch (PollState)
			{
				case 0: 
					PowerPoll();
					VideoMutePoll();
					break;
				case 1:
					PowerPoll();
					VideoMutePoll();
					InputPoll();
					break;
				case 2:
					PowerPoll();
					VideoMutePoll();
					break;
				case 3:
					PowerPoll();
					VideoMutePoll();
					LightUsagePoll();
					PollState = 0;
					break;
				default:
					PollState = 0;
					return;
			}PollState++; 
		}

	    private void AddRoutingInputPort(RoutingInputPort port, string fbMatch)
	    {
	        port.FeedbackMatchObject = fbMatch;
            InputPorts.Add(port);
	    }

        private int GetInputNumberFromName(string name)
        {
            switch(name)
            {
                case "hdmi1":
                case "hdmiIn1":
                    return(1);
                case "hdmi2":
                case "hdmiIn2":
                    return(3);
                case "computer":
                case "rgbIn1":
                    return(2);
                default:
                    return (0);
            }
        }

		private string GetInputNameFromNumber(int inputNumber)
		{
			switch (inputNumber)
			{
				case 1:
					return NecAsciiInputs.HdmiIn1;
				case 2:
					return NecAsciiInputs.RgbIn1;
				case 3:
					return NecAsciiInputs.HdmiIn2;
				default:
					return null;
			}
		}

		private void SyncSelectableInputs()
		{
			var selectedInput = GetInputNameFromNumber(InputNumber);
			if (string.IsNullOrEmpty(selectedInput))
			{
				return;
			}

			_inputs.SetCurrentItemFromFeedback(selectedInput);
		}

	    private void InitializeRoutingInputPorts()
	    {
			InputFeedback = new List<BoolFeedback>();
	        AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn1, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, new Action(() => InputSelect(new NecAsciiCommand(RoutingPortNames.HdmiIn1))),this), InputList[RoutingPortNames.HdmiIn1]);
	        AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.HdmiIn2, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Hdmi, new Action(() => InputSelect(new NecAsciiCommand(RoutingPortNames.HdmiIn2))), this), InputList[RoutingPortNames.HdmiIn2]);
	        AddRoutingInputPort(new RoutingInputPort(RoutingPortNames.RgbIn1, eRoutingSignalType.AudioVideo, eRoutingPortConnectionType.Rgb, new Action(() => InputSelect(new NecAsciiCommand(RoutingPortNames.RgbIn1))), this), InputList[RoutingPortNames.RgbIn1]);
			for (var i = 0; i < InputPorts.Count; i++)
			{
				var j = i;

				InputFeedback.Add(new BoolFeedback(() => InputNumber == j + 1));
			}
			InputNumberFeedback = new IntFeedback(() =>
			{
				return InputNumber;
			});

			SyncSelectableInputs();
	    }


	    public void InputSelect(NecAsciiCommand command)
	    {
			
	        if (command == null) return;

	        var commandString = command.Command;

	        if (commandString == string.Empty ||!InputList.ContainsKey(commandString)) return;

            //Input already selected, prevent resend which will blink display for a second
            if (InputNumber == GetInputNumberFromName(commandString)) return;

            SendText(string.Format("input {0}",InputList[commandString]));

            // Optimistically update feedback immediately, real feedback from device will override
            InputNumber = GetInputNumberFromName(commandString);
            SyncSelectableInputs();

			new CTimer((o) => InputPoll(), 300);
			//InputPoll();
	    }
		public override void ExecuteSwitch(object selector)
	    {
			Debug.Console(2, this, "ExecuteSwitch");
			if (selector is Action)
				(selector as Action).Invoke();
			else
				Debug.Console(1, this, "WARNING: ExecuteSwitch cannot handle type {0}", selector.GetType());
			/*
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
			*/ 
	    }

		public void InputPoll()
		{
			SendText("input");
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
			get
			{
				return () =>
				{
					var inputString = "";

					switch (InputNumber)
					{
						case 1:
							inputString = RoutingPortNames.HdmiIn1;
							break;
						case 2:
							inputString = RoutingPortNames.RgbIn1;
							break;
						case 3:
							inputString = RoutingPortNames.HdmiIn2;
							break;
					}
					return inputString;

				};
			}
		}

		public ISelectableItems<string> Inputs => _inputs;

        #region Commands

        /// <summary>
        /// 
        /// </summary>
        public override void PowerOn()
        {
            //if (PowerIsOn || IsWarming || IsCooling) return;

	        SendText("power on");

				IsCooling = false;
                PowerIsOn = true;
                IsWarming = true;

			new CTimer((o) => PowerPoll(), 300);
			//PowerPoll();
        }

		/// <summary>
		/// 
		/// </summary>
	    public override void PowerOff()
	    {
	        //if (!PowerIsOn || IsWarming || IsCooling) return;

            SendText("power off");

	        PowerIsOn = false;
			 IsCooling = true;
			 IsWarming = false;
			new CTimer((o) => PowerPoll(), 300);
			//PowerPoll();
	    }

		/// <summary>
		/// 
		/// </summary>
	    public override void PowerToggle()
	    {
	        if (PowerIsOn)
	        {
	            PowerOff();
	            return;
	        }
            PowerOn();
	    }
		
		/// <summary>
		/// 
		/// </summary>
		public void PowerPoll()
		{
			SendText("power");
		}

		/// <summary>
		/// 
		/// </summary>
		public void VideoMuteOn()
        {
	        SendText("shutter close");

			new CTimer((o) => VideoMutePoll(), 300);
			//VideoMutePoll();
        }

		/// <summary>
		/// 
		/// </summary>
		public void VideoMuteOff()
        {
	        SendText("shutter open");

			new CTimer((o) => VideoMutePoll(), 300);
			//VideoMutePoll();
        }
		/// <summary>
		/// 
		/// </summary>
		public void VideoMuteToggle()
		{
			if(VideoMuteState)
				VideoMuteOff();
			else 
				VideoMuteOn();
		}

		public void VideoMutePoll()
		{
			SendText("shutter");
		}
		public void LightUsagePoll()
		{
			SendText("usage light hours");
		}
		public void FreezeImageOn()
		{
			SendText("freeze on");

			new CTimer((o) => FreezeImagePoll(), 300);
			//FreezeImagePoll();
		}
		public void FreezeImageOff()
		{
			SendText("freeze off");

			new CTimer((o) => FreezeImagePoll(), 300);
			//FreezeImagePoll();
		}
		public void FreezeImageToggle()
		{
			if (FreezeImageState)
				FreezeImageOff();
			else
				FreezeImageOn();
		}
		public void FreezeImagePoll()
		{
			SendText("freeze");
		}
		public void LensFunction(eLensFunction function) 
		{
			switch (function)
			{
				case eLensFunction.ZoomPlus: SendText("lens zoom start +"); break;
				case eLensFunction.ZoomMinus: SendText("lens zoom start -"); break;
				case eLensFunction.ZoomStop: SendText("lens zoom stop"); break;
				case eLensFunction.FocusPlus: SendText("lens focus start +"); break;
				case eLensFunction.FocusMinus: SendText("lens focus start -"); break;
				case eLensFunction.FocusStop: SendText("lens focus stop"); break;
				case eLensFunction.HShiftPlus: SendText("lens h_shift start +"); break;
				case eLensFunction.HShiftMinus: SendText("lens h_shift start -"); break;
				case eLensFunction.HShiftStop: SendText("lens h_shift stop"); break;
				case eLensFunction.VShiftPlus: SendText("lens v_shift start +"); break;
				case eLensFunction.VShiftMinus: SendText("lens v_shift start -"); break;
				case eLensFunction.VShiftStop: SendText("lens v_shift stop"); break;
				case eLensFunction.Home: SendText("lens home"); break;
			}	
		}
        #endregion Commands 

        #region IBridgeAdvanced Members
		
        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new NecCommonAsciiDevicePluginBridgeJoinMap(joinStart);
            LinkDisplayToApi(this, trilist, joinStart, joinMapKey, bridge);

            this.LampHoursFeedback.LinkInputSig(trilist.UShortInput[joinMap.LampHours.JoinNumber]);
            this.LampHoursStringFeedback.LinkInputSig(trilist.StringInput[joinMap.LampHours.JoinNumber]);
			
			trilist.SetSigTrueAction(joinMap.Freeze.JoinNumber, () => FreezeImageToggle());
			FreezeImageIsOnFeedBack.LinkInputSig(trilist.BooleanInput[joinMap.Freeze.JoinNumber]);

			// input analog feedback
			InputNumberFeedback.LinkInputSig(trilist.UShortInput[joinMap.InputSelect.JoinNumber]);
			// Input digitals
			var count = 0;
			foreach (var input in InputPorts)
			{
				var i = count;
				InputFeedback[count].LinkInputSig(
					trilist.BooleanInput[joinMap.InputSelectOffset.JoinNumber + (uint)count]);
				count++;
			}

			trilist.SetSigTrueAction(joinMap.VideoMuteOn.JoinNumber, () => VideoMuteOn());
			trilist.SetSigTrueAction(joinMap.VideoMuteOff.JoinNumber, () => VideoMuteOff());
			trilist.SetSigTrueAction(joinMap.VideoMuteToggle.JoinNumber, () => VideoMuteToggle());
			VideoMuteIsOnFeedBack.LinkInputSig(trilist.BooleanInput[joinMap.VideoMuteOn.JoinNumber]);
			VideoMuteIsOnFeedBack.LinkInputSig(trilist.BooleanInput[joinMap.VideoMuteToggle.JoinNumber]);
			VideoMuteIsOnFeedBack.LinkComplementInputSig(trilist.BooleanInput[joinMap.VideoMuteOff.JoinNumber]);
			
			trilist.SetBoolSigAction(joinMap.HShiftPlus.JoinNumber, (b) =>
				{
					if (b) LensFunction(eLensFunction.HShiftPlus);
					else LensFunction(eLensFunction.HShiftStop);
				});
			trilist.SetBoolSigAction(joinMap.HShiftMinus.JoinNumber, (b) =>
			{
				if (b) LensFunction(eLensFunction.HShiftMinus);
				else LensFunction(eLensFunction.HShiftStop);
			});
			trilist.SetBoolSigAction(joinMap.VShiftPlus.JoinNumber, (b) =>
			{
				if (b) LensFunction(eLensFunction.VShiftPlus);
				else LensFunction(eLensFunction.VShiftStop);
			});
			trilist.SetBoolSigAction(joinMap.VShiftMinus.JoinNumber, (b) =>
			{
				if (b) LensFunction(eLensFunction.VShiftMinus);
				else LensFunction(eLensFunction.VShiftStop);
			});
			trilist.SetBoolSigAction(joinMap.ZoomPlus.JoinNumber, (b) =>
			{
				if (b) LensFunction(eLensFunction.ZoomPlus);
				else LensFunction(eLensFunction.ZoomStop);
			});
			trilist.SetBoolSigAction(joinMap.ZoomMinus.JoinNumber, (b) =>
			{
				if (b) LensFunction(eLensFunction.ZoomMinus);
				else LensFunction(eLensFunction.ZoomStop);
			});
			trilist.SetBoolSigAction(joinMap.FocusPlus.JoinNumber, (b) =>
			{
				if (b) LensFunction(eLensFunction.FocusPlus);
				else LensFunction(eLensFunction.FocusStop);
			});
			trilist.SetBoolSigAction(joinMap.FocusMinus.JoinNumber, (b) =>
			{
				if (b) LensFunction(eLensFunction.FocusMinus);
				else LensFunction(eLensFunction.FocusStop);
			});
			
        }

        #endregion


	}
	public enum eLensFunction
	{
		ZoomPlus,
		ZoomMinus,
		ZoomStop,
		FocusPlus,
		FocusMinus,
		FocusStop,
		HShiftPlus,
		HShiftMinus,
		HShiftStop, 
		VShiftPlus, 
		VShiftMinus,
		VShiftStop,
		Home
	}

	public class NecAsciiInputs: ISelectableItems<string>
	{
		public const string HdmiIn1 = "hdmiIn1";
		public const string HdmiIn2 = "hdmiIn2";
		public const string RgbIn1 = "rgbIn1";
		private const string HdmiIn1Label = "HDMI 1";
		private const string HdmiIn2Label = "HDMI 2";
		private const string RgbIn1Label = "RGB";

		private Dictionary<string, ISelectableItem> _items;
		private string _currentItem;
		private bool _isApplyingFeedback;

		public NecAsciiInputs()
		{
			_items = new Dictionary<string, ISelectableItem>(StringComparer.OrdinalIgnoreCase)
			{
				{ HdmiIn1, new NecAsciiSelectableItem(HdmiIn1, HdmiIn1Label) },
				{ HdmiIn2, new NecAsciiSelectableItem(HdmiIn2, HdmiIn2Label) },
				{ RgbIn1, new NecAsciiSelectableItem(RgbIn1, RgbIn1Label) }
			};

			 foreach (var item in _items) //This creates delegates for each item that fire when item.Value.ItemUpdated is invoked
				{
					item.Value.ItemUpdated += (sender, args) => {
						if (_isApplyingFeedback) return; // prevents feedback from looping back into InputSelect, no need for guard
						if ((sender as ISelectableItem)?.IsSelected == true)
							CurrentItem = item.Key;
					};
				}
		}

        public Dictionary<string, ISelectableItem> Items
        {
            get => _items;
            set
            {
                _items = value ?? new Dictionary<string, ISelectableItem>(StringComparer.OrdinalIgnoreCase);
                ItemsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        public string CurrentItem
        {
            get => _currentItem;
            set
            {
                if (string.IsNullOrEmpty(value) || !_items.ContainsKey(value) || string.Equals(_currentItem, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _currentItem = value;
                CurrentItemChanged?.Invoke(this, EventArgs.Empty);
            }
        }

		public void SetCurrentItemFromFeedback(string value)
		{
			if (string.IsNullOrEmpty(value) || !_items.ContainsKey(value))
			{
				return;
			}

			var changed = !string.Equals(_currentItem, value, StringComparison.OrdinalIgnoreCase);
			_isApplyingFeedback = true;
			try
			{
				foreach (var item in _items)
				{
					item.Value.IsSelected = string.Equals(item.Key, value, StringComparison.OrdinalIgnoreCase);
				}
			}
			finally
			{
				_isApplyingFeedback = false;
			}

			_currentItem = value;

			if (changed)
			{
				CurrentItemChanged?.Invoke(this, EventArgs.Empty);
			}

			ItemsUpdated?.Invoke(this, EventArgs.Empty);
		}

        public event EventHandler ItemsUpdated;
        public event EventHandler CurrentItemChanged;

		private class NecAsciiSelectableItem : ISelectableItem
		{
			private bool _isSelected;
			private readonly string _key;
			private readonly string _name;

			public NecAsciiSelectableItem(string key, string name)
			{
				_key = key;
				_name = name;
			}

			public string Key => _key;
			public string Name => _name;

			public bool IsSelected
			{
				get => _isSelected;
				set
				{
					if (_isSelected == value) return;
					_isSelected = value;
					ItemUpdated?.Invoke(this, EventArgs.Empty);
				}
			}

			public event EventHandler ItemUpdated;

			public void Select() => IsSelected = true;
		}
    }
}