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
		private CTimer _powerPollTimer;
		private CTimer _powerPollTimeoutTimer;
		// Latest input switch not yet made: runs at once when power is on, otherwise when power on is reported.
		private Action _pendingSwitch;
		private readonly object _switchLock = new object();
		// An input selected while it already showed as selected. Until _inputToConfirmUntil, an input reply showing
		// another input switches to it. Guarded by _inputLock.
		private string _inputToConfirm;
		private DateTime _inputToConfirmUntil;
		private const int InputConfirmWindowMs = 5000;
		private readonly object _inputLock = new object();

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
				this.LogVerbose("newInputNumber {0}", _inputNumber);
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

		// Commands go out at least this far apart; see NecCommandQueue.
		private const int CommandGapMs = 300;
		private readonly NecCommandQueue _commandQueue;

		public NecCommonAsciiDevice(string key, string name, NecCommonAsciiDeviceConfigObject config, IBasicCommunication comms)
			: base(key, name)
		{
			this.LogInformation("Constructing new {0} instance", name);

			_config = config;
			var exposedInputs = GetExposedInputs(config.Inputs);

			_inputs = new NecAsciiInputs(exposedInputs);
			//When the CurrentItemChanged event is invoked, it calls the InputSelect method with the CurrentItem string
			// Only selections from a user or the API switch the display; feedback changes don't.
			_inputs.ItemSelected += (sender, key) => InputSelect(new NecAsciiCommand(key));

			// TODO [ ] Update the constructor as needed for the plugin device being developed

		    InputList = new Dictionary<string, string>()
		    {
               {RoutingPortNames.HdmiIn,    "hdmi1"},
               {RoutingPortNames.HdmiIn1,   "hdmi1"},
               {RoutingPortNames.HdmiIn2,   "hdmi2"},
               {RoutingPortNames.RgbIn,    "computer"},
               {RoutingPortNames.RgbIn1,   "computer"},
               {NecAsciiInputs.HdbaseT1,   "hdbaset"}

		    };
			InitializeRoutingInputPorts(exposedInputs);

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
			_commandQueue = new NecCommandQueue(
				CommandGapMs,
				text => Comms.SendText(string.Format("{0}{1}", text, CommsDelimiter)),
				(ms, elapsed) => new CTimer(o => elapsed(), ms),
				(text, ex) => this.LogError("Unable to send '{0}': {1}", text, ex.Message));

			CommunicationMonitor = new GenericCommunicationMonitor(this, Comms, 5000, 120000, 300000, Poll);
			CommunicationMonitor.StatusChange += CommunicationMonitor_StatusChange;

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

			this.LogInformation("Constructing new {0} instance complete", name);
			this.LogInformation(new string('*', 80));
			this.LogInformation(new string('*', 80));
		}

      	private void PowerIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            this.LogInformation("PowerIsOnFeedback changed to {0}", PowerIsOnFeedback.BoolValue);
            IsWarming = false;
            IsCooling = false;

            StopPowerPollTimer();

            if (PowerIsOn)
                RunPendingSwitch("power on reported");
            else
                CancelPendingSwitch("power went off");
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
        protected override bool CustomActivate()
		{
			// Essentials will handle the connect method to the device                       
			Comms.Connect();
			// Essentialss will handle starting the comms monitor
			CommunicationMonitor.Start();

			return base.CustomActivate();
		}

		private void CommunicationMonitor_StatusChange(object sender, MonitorStatusChangeEventArgs args)
		{
			if (OnlineFeedback != null)
				OnlineFeedback.FireUpdate();

			if (MonitorStatusFeedback != null)
				MonitorStatusFeedback.FireUpdate();
		}

		private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
		{
			// Commands queued before a drop are stale by the time it reconnects.
			if (!Comms.IsConnected)
				_commandQueue.Clear();

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
			// This runs on the comms receive thread: an exception escaping here stops all further
			// replies from being processed until the connection is re-established.
			try
			{
				ProcessLine(args);
			}
			catch (Exception ex)
			{
				this.LogError("Handle_LineRecieved: unable to process '{0}': {1}", args.Text, ex.Message);
				this.LogVerbose(ex, "Handle_LineRecieved exception");
			}
		}

		private void ProcessLine(GenericCommMethodReceiveTextArgs args)
		{
			// TODO [ ] Implement method 

			if (args.Text.Contains("error:busy") && _powerPollTimer != null) //device often sends this instead of power warming
			{
				IsCooling = false;
				IsWarming = true;
				return;
			}

			if (args.Text.Contains(" "))
			{
				var dataReceived = args.Text;
				var parts = dataReceived.Split(' ');

				var responseType = parts[0].ToLower();
				var responseValue = GetCurrentValue(parts[1].ToLower());
				this.LogVerbose("responseType ={0} responseValue ={1}", responseType, responseValue);
				if (responseType.Contains("power")) UpdatePower(responseValue);
				else if (responseType.Contains("input")) UpdateInput(responseValue);
				else if (responseType.Contains("status")) UpdateStatus(responseValue);
				else if (responseType.Contains("shutter"))
				{
					switch (responseValue)
					{
						case "open":
							VideoMuteState = false;
							break;
						case "close":
							VideoMuteState = true;
							break;
					}
				}
				else if (responseType.Contains("freeze"))
				{
					switch (responseValue)
					{
						case "off":
							FreezeImageState = false;
							break;
						case "on":
							FreezeImageState = true;
							break;
					}
				}
				else if (responseType.Contains("usage"))
				{
					// e.g. "usage light hours 40000"
					int hours;
					if (parts.Length > 3 && int.TryParse(parts[3].Trim(), out hours))
					{
						LampHours = hours;
					}
					else
					{
						this.LogWarning("Unable to parse lamp hours from '{0}'", dataReceived);
					}
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
				this.LogInformation("Exception Here - {0}", e.Message);
			}
		}
        private void UpdateStatus(string response)
        {
            const char splitChar = ';';

            var statusMsg = response.Split(splitChar);

            UpdatePower(statusMsg[0].ToLower());
        }

		// Replies to "<command> ?" look like "cur=off,sel=on|off"; the current value follows "cur=".
		private static string GetCurrentValue(string value)
		{
			value = value.Trim();
			return value.StartsWith("cur=") ? value.Substring(4).Split(',')[0] : value;
		}

	    private void UpdatePower(string response)
	    {
			// Match the whole value: help replies such as "[on|off|?]" must not change the power state.
			switch (response.Trim())
			{
				case "on":
					PowerIsOn = true;
					break;
				case "off":
					PowerIsOn = false;
					break;
				case "warming":
				case "busy":
					IsCooling = false;
					IsWarming = true;
					break;
				case "cooling":
					IsWarming = false;
					IsCooling = true;
					break;
				case "running":
					IsWarming = false;
					IsCooling = false;
					break;
				case "standby":
					IsWarming = false;
					IsCooling = false;
					PowerIsOn = false;
					break;
				default:
					this.LogVerbose("UpdatePower: ignoring '{0}'", response);
					break;
			}
	    }

	    private void UpdateInput(string response)
	    {
			this.LogVerbose("UpdateInput {0}", response);

			// Under _inputLock with InputSelect, so a confirmation can't send an input after a newer selection.
			lock (_inputLock)
			{
				InputNumber = GetInputNumberFromName(response);
				SyncSelectableInputs();

				if (_inputToConfirm == null)
					return;

				if (DateTime.UtcNow > _inputToConfirmUntil)
				{
					_inputToConfirm = null;
					return;
				}

				// A reply showing the selected input doesn't end the confirmation: it may answer a query sent before
				// the selection. The first reply in the window showing another input switches to the selected one.
				if (GetInputNumberFromName(_inputToConfirm) != InputNumber)
				{
					var toConfirm = _inputToConfirm;
					_inputToConfirm = null;
					this.LogInformation("InputSelect - display reports '{0}', not the selected '{1}'; switching", response, toConfirm);
					InputSelect(new NecAsciiCommand(toConfirm));
				}
			}
	    }
	
		public void SendText(string text)
		{
			if (string.IsNullOrEmpty(text)) return;

			_commandQueue.Enqueue(text);
		}

		/// <summary>
		/// Sends a status query. It's dropped if the same query is already waiting to go out.
		/// </summary>
		private void SendQuery(string text)
		{
			_commandQueue.Enqueue(text, skipIfPending: true);
		}

		/// <summary>
		/// Polls the device
		/// </summary>
		/// <remarks>
		/// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed
		/// </remarks>
		public void Poll()
		{
			if (PollState < 0 || PollState >= NecPollPlan.StateCount)
				PollState = 0;

			foreach (var command in NecPollPlan.GetCommands(PollState, PowerIsOn, _config.PollShutter))
				SendQuery(command);

			PollState = (PollState + 1) % NecPollPlan.StateCount;
		}

	    private void AddRoutingInputPort(RoutingInputPort port, string fbMatch)
	    {
	        port.FeedbackMatchObject = fbMatch;
            InputPorts.Add(port);
	    }

        private int GetInputNumberFromName(string name)
        {
            return NecInputMap.GetInputNumber(name);
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
				case 4:
					return NecAsciiInputs.HdbaseT1;
				default:
					return null;
			}
		}

		private void SyncSelectableInputs()
		{
			// An input that isn't exposed (e.g. RGB picked on the remote) or isn't recognised leaves no item selected.
			var selectedInput = GetInputNameFromNumber(InputNumber);
			if (string.IsNullOrEmpty(selectedInput) || !_inputs.Items.ContainsKey(selectedInput))
			{
				_inputs.ClearCurrentItemFromFeedback();
				return;
			}

			_inputs.SetCurrentItemFromFeedback(selectedInput);
		}

		/// <summary>
		/// The inputs to expose, in the fixed order hdmiIn1, hdmiIn2, rgbIn1, hdbaseT1, with their labels. The defaults when
		/// the config lists none.
		/// </summary>
		private List<KeyValuePair<string, string>> GetExposedInputs(List<NecInputConfig> configured)
		{
			var unknownKeys = new List<string>();
			var exposed = NecInputMap.GetExposedInputs(configured, unknownKeys);

			foreach (var key in unknownKeys)
				this.LogWarning("Ignoring unknown input '{0}' in config. Known inputs: {1}", key, string.Join(", ", NecInputMap.AllInputs.Select(i => i.Key)));

			if (unknownKeys.Count > 0 && unknownKeys.Count == configured.Count(i => i != null))
				this.LogWarning("No known inputs in config; exposing the default inputs");

			return exposed;
		}

	    private void InitializeRoutingInputPorts(List<KeyValuePair<string, string>> exposedInputs)
	    {
			InputFeedback = new List<BoolFeedback>();
			foreach (var input in exposedInputs)
			{
				// Input keys match the routing port names (hdmiIn1, hdmiIn2, rgbIn1, hdbaseT1).
				var portKey = input.Key;
				var connectionType = portKey == RoutingPortNames.RgbIn1 ? eRoutingPortConnectionType.Rgb
					: portKey == NecAsciiInputs.HdbaseT1 ? eRoutingPortConnectionType.HdBaseT
					: eRoutingPortConnectionType.Hdmi;
				AddRoutingInputPort(new RoutingInputPort(portKey, eRoutingSignalType.AudioVideo, connectionType, new Action(() => InputSelect(new NecAsciiCommand(portKey))), this), InputList[portKey]);

				// Bridge joins select inputs by port position, so each feedback follows its port rather than
				// the projector's input number (hdmi1 = 1, computer = 2, hdmi2 = 3, hdbaset = 4).
				InputFeedback.Add(new BoolFeedback(() => InputNumber != 0 && InputNumber == GetInputNumberFromName(portKey)));
			}

			InputNumberFeedback = new IntFeedback(GetSelectedPortNumber);

			SyncSelectableInputs();
	    }

		/// <summary>
		/// 1-based position of the selected input among the exposed ports, matching the bridge's analog
		/// InputSelect join. 0 when the selected input isn't exposed.
		/// </summary>
		private int GetSelectedPortNumber()
		{
			return NecInputMap.GetPortNumber(InputPorts.Select(p => p.Key), InputNumber);
		}


	    public void InputSelect(NecAsciiCommand command)
	    {
			this.LogVerbose("InputSelect called. Command: {0}, PowerIsOn: {1}",
				command?.Command ?? "null", PowerIsOn);

	        if (command == null) { this.LogVerbose("InputSelect - command is null, returning"); return; }

	        var commandString = command.Command;

	        if (commandString == string.Empty ||!InputList.ContainsKey(commandString)) { this.LogVerbose("InputSelect - command '{0}' is empty or not in InputList, returning", commandString); return; }

			// Selections and confirmations (UpdateInput) are serialized, so the newest selection is the last sent.
			lock (_inputLock)
			{
	            // Input already shows as selected. Resending would blink the display for a second, but the feedback can
	            // be stale (input polls every 10 s, and some displays answer "error:busy" just after an input change).
	            // Query the input instead; UpdateInput sends the switch if the display reports another one in time.
	            if (InputNumber == GetInputNumberFromName(commandString))
	            {
	                this.LogInformation("InputSelect - input '{0}' already shows as selected (InputNumber: {1}); querying input to confirm", commandString, InputNumber);
	                _inputToConfirm = commandString;
	                _inputToConfirmUntil = DateTime.UtcNow.AddMilliseconds(InputConfirmWindowMs);
	                InputPoll();
	                return;
	            }

	            // A newer selection replaces one waiting to be confirmed.
	            _inputToConfirm = null;

	            this.LogVerbose("InputSelect - Sending 'input {0}' to device", InputList[commandString]);
	            SendText(string.Format("input {0}",InputList[commandString]));

	            // Optimistically update feedback immediately, real feedback from device will override
	            InputNumber = GetInputNumberFromName(commandString);
	            SyncSelectableInputs();
			}

			new CTimer((o) => InputPoll(), 300);
			//InputPoll();
	    }
		public override void ExecuteSwitch(object selector)
	    {
			this.LogVerbose("ExecuteSwitch called. Selector type: {0}, PowerIsOn: {1}, IsWarming: {2}",
				selector?.GetType().Name ?? "null", PowerIsOn, IsWarming);

			var inputSelector = selector as Action;
			if (inputSelector == null)
			{
				this.LogWarning("ExecuteSwitch - Unable to switch using selector {0}", selector);
				return;
			}

			// Every switch goes through _pendingSwitch, replacing any earlier one still waiting, so only the
			// latest selection is made. It runs now if power is on, otherwise when power on is reported.
			lock (_switchLock)
			{
				_pendingSwitch = inputSelector;
			}

			// Checked after storing the switch: if power on was reported in between, the feedback handler found
			// nothing to run.
			if (PowerIsOn)
			{
				RunPendingSwitch("power is on");
				return;
			}

			this.LogVerbose("ExecuteSwitch - Power is off, queuing input switch and powering on");
			PowerOn();
	    }

		// Runs under _switchLock so a switch run from the power feedback and one from ExecuteSwitch can't
		// overlap and finish out of order.
		private void RunPendingSwitch(string reason)
		{
			lock (_switchLock)
			{
				var pending = _pendingSwitch;
				_pendingSwitch = null;
				if (pending == null) return;

				this.LogVerbose("ExecuteSwitch - {0}, invoking input switch", reason);
				pending();
			}
		}

		private void CancelPendingSwitch(string reason)
		{
			lock (_switchLock)
			{
				if (_pendingSwitch == null) return;
				_pendingSwitch = null;
			}

			this.LogWarning("ExecuteSwitch - Dropping queued input switch: {0}", reason);
		}

		public void InputPoll()
		{
			SendQuery("input");
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
						case 4:
							inputString = NecAsciiInputs.HdbaseT1;
							break;
					}
					return inputString;

				};
			}
		}

		public ISelectableItems<string> Inputs => _inputs;

        #region Commands

		private void StartPowerPollTimer(uint configuredTimeMs)
		{
			StopPowerPollTimer();
			var timeoutMs = NecPowerTransition.GetTimeoutMs(configuredTimeMs);
			_powerPollTimer = new CTimer((o) => PowerPoll(), null, NecPowerTransition.PollIntervalMs, NecPowerTransition.PollIntervalMs);
			_powerPollTimeoutTimer = new CTimer((o) =>
			{
				this.LogWarning("PowerPoll timed out after {0}ms with no power feedback; clearing warming/cooling", timeoutMs);
				_powerPollTimeoutTimer = null;
				StopPowerPollTimer();

				// Without this, warming or cooling would stay set until the next comm monitor poll sees a
				// change in power. An input switch queued in ExecuteSwitch stays queued: a slow display still
				// comes up on the selected input when the comm monitor's poll sees power on.
				IsWarming = false;
				IsCooling = false;
				if (_pendingSwitch != null)
					this.LogWarning("ExecuteSwitch - Queued input switch will run when power on is reported");
				PowerPoll();
			}, null, timeoutMs, Timeout.Infinite);
		}

		private void StopPowerPollTimer()
		{
			_powerPollTimer?.Stop();
			_powerPollTimer?.Dispose();
			_powerPollTimer = null;

			_powerPollTimeoutTimer?.Stop();
			_powerPollTimeoutTimer?.Dispose();
			_powerPollTimeoutTimer = null;
		}

        /// <summary>
        /// 
        /// </summary>
        public override void PowerOn()
        {
            this.LogVerbose("PowerOn called. PowerIsOn: {0}, _powerPollTimer is null: {1}",
                PowerIsOn, _powerPollTimer == null);
            // _powerPollTimer running indicates a power transition is already in progress
            if (PowerIsOn || _powerPollTimer != null) { this.LogVerbose("PowerOn - skipping (already on or transition in progress)"); return; }

            this.LogVerbose("PowerOn - Sending 'power on' to device");
	        SendText("power on");

			StartPowerPollTimer(_warmingTimeMs);
			//PowerPoll();
        }

		/// <summary>
		/// 
		/// </summary>
	    public override void PowerOff()
	    {
	        // Power off overrides an input switch still waiting for power on.
	        CancelPendingSwitch("power off requested");

	        // _powerPollTimer running indicates a power transition is already in progress
	        if (!PowerIsOn || _powerPollTimer != null) return;

            SendText("power off");

			StartPowerPollTimer(_coolingTimeMs);
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
			SendQuery("power");
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
			SendQuery("shutter");
		}
		public void LightUsagePoll()
		{
			SendQuery("usage light hours");
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
			SendQuery("freeze");
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
		public const string HdbaseT1 = "hdbaseT1";

		private Dictionary<string, ISelectableItem> _items;
		private string _currentItem;
		private bool _isApplyingFeedback;

		public NecAsciiInputs(IEnumerable<KeyValuePair<string, string>> inputs)
		{
			_items = new Dictionary<string, ISelectableItem>(StringComparer.OrdinalIgnoreCase);
			foreach (var input in inputs)
			{
				var key = input.Key;
				var item = new NecAsciiSelectableItem(key, input.Value);
				item.ItemUpdated += (sender, args) =>
				{
					if (_isApplyingFeedback) return; // prevents feedback from looping back into InputSelect
					if (item.IsSelected)
						SelectItem(key);
				};
				// Selecting the item that already shows as selected still asks for it: the feedback may be stale,
				// for example while the display answers "error:busy" just after an input change from its remote.
				item.Reselected += (sender, args) => SelectItem(key);
				_items[key] = item;
			}
		}

		/// <summary>
		/// Raised when a user or the API selects an input, including the one already shown as selected.
		/// Not raised for feedback from the display.
		/// </summary>
		public event EventHandler<string> ItemSelected;

		private void SelectItem(string key)
		{
			if (!string.Equals(_currentItem, key, StringComparison.OrdinalIgnoreCase))
			{
				_currentItem = key;
				CurrentItemChanged?.Invoke(this, EventArgs.Empty);
			}

			ItemSelected?.Invoke(this, key);
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
                if (string.IsNullOrEmpty(value) || !_items.ContainsKey(value))
                {
                    return;
                }

                SelectItem(value);
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

		/// <summary>
		/// Deselects every item, for when the device reports an input that isn't in the list
		/// </summary>
		public void ClearCurrentItemFromFeedback()
		{
			if (_currentItem == null && !_items.Values.Any(i => i.IsSelected))
			{
				return;
			}

			_isApplyingFeedback = true;
			try
			{
				foreach (var item in _items)
				{
					item.Value.IsSelected = false;
				}
			}
			finally
			{
				_isApplyingFeedback = false;
			}

			_currentItem = null;
			CurrentItemChanged?.Invoke(this, EventArgs.Empty);
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

			/// <summary>
			/// Raised by Select() when the item is already selected
			/// </summary>
			public event EventHandler Reselected;

			public void Select()
			{
				if (_isSelected)
				{
					Reselected?.Invoke(this, EventArgs.Empty);
					return;
				}

				IsSelected = true;
			}
		}
    }
}