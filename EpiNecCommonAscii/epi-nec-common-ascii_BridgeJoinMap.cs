using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace EpiNecCommonAscii
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.  Reference Essentials JoinMaps, if one exists for the device plugin being developed
	/// </remarks>
	/// <see cref="PepperDash.Essentials.Core.Bridges"/>
	/// <example>
	/// "EssentialsPluginBridgeJoinMapTemplate" renamed to "SamsungMdcBridgeJoinMap"
	/// </example>
	public class NecCommonAsciiDevicePluginBridgeJoinMap : DisplayControllerJoinMap
	{

		#region From Base 
		/*
		 *         [JoinName("Name")]
        public JoinDataComplete Name = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 },
            new JoinMetadata { Description = "Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

        [JoinName("PowerOff")]
        public JoinDataComplete PowerOff = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 }, 
            new JoinMetadata { Description = "Power Off", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

        [JoinName("PowerOn")]
        public JoinDataComplete PowerOn = new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 }, 
            new JoinMetadata { Description = "Power On", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });

        [JoinName("IsTwoWayDisplay")]
        public JoinDataComplete IsTwoWayDisplay = new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 }, 
            new JoinMetadata { Description = "Is Two Way Display", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

        [JoinName("VolumeUp")]
        public JoinDataComplete VolumeUp = new JoinDataComplete(new JoinData { JoinNumber = 5, JoinSpan = 1 }, 
            new JoinMetadata { Description = "Volume Up", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

        [JoinName("VolumeLevel")]
        public JoinDataComplete VolumeLevel = new JoinDataComplete(new JoinData { JoinNumber = 5, JoinSpan = 1 },
            new JoinMetadata { Description = "Volume Level", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Analog });

        [JoinName("VolumeDown")]
        public JoinDataComplete VolumeDown = new JoinDataComplete(new JoinData { JoinNumber = 6, JoinSpan = 1 }, 
            new JoinMetadata { Description = "Volume Down", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

        [JoinName("VolumeMute")]
        public JoinDataComplete VolumeMute = new JoinDataComplete(new JoinData { JoinNumber = 7, JoinSpan = 1 }, 
            new JoinMetadata { Description = "Volume Mute", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });

        [JoinName("VolumeMuteOn")]
        public JoinDataComplete VolumeMuteOn = new JoinDataComplete(new JoinData { JoinNumber = 8, JoinSpan = 1 }, 
            new JoinMetadata { Description = "Volume Mute On", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });

        [JoinName("VolumeMuteOff")]
        public JoinDataComplete VolumeMuteOff = new JoinDataComplete(new JoinData { JoinNumber = 9, JoinSpan = 1 }, 
            new JoinMetadata { Description = "Volume Mute Off", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });

        [JoinName("InputSelectOffset")]
        public JoinDataComplete InputSelectOffset = new JoinDataComplete(new JoinData { JoinNumber = 11, JoinSpan = 10 }, 
            new JoinMetadata { Description = "Input Select", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });

        [JoinName("InputNamesOffset")]
        public JoinDataComplete InputNamesOffset = new JoinDataComplete(new JoinData { JoinNumber = 11, JoinSpan = 10 }, 
            new JoinMetadata { Description = "Input Names Offset", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

        [JoinName("InputSelect")]
        public JoinDataComplete InputSelect = new JoinDataComplete(new JoinData { JoinNumber = 11, JoinSpan = 1 },
            new JoinMetadata { Description = "Input Select", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Analog });

        [JoinName("ButtonVisibilityOffset")]
        public JoinDataComplete ButtonVisibilityOffset = new JoinDataComplete(new JoinData { JoinNumber = 41, JoinSpan = 10 }, 
            new JoinMetadata { Description = "Button Visibility Offset", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.DigitalSerial });

        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline = new JoinDataComplete(new JoinData { JoinNumber = 50, JoinSpan = 1 }, 
            new JoinMetadata { Description = "Is Online", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		 */
		#endregion 
		#region Digital
		[JoinName("Blank")]
		public JoinDataComplete Blank = new JoinDataComplete(new JoinData { JoinNumber = 36, JoinSpan = 1 },
			new JoinMetadata { Description = "Blank", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("Freeze")]
		public JoinDataComplete Freeze = new JoinDataComplete(new JoinData { JoinNumber = 37, JoinSpan = 1 },
			new JoinMetadata { Description = "Freeze", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("VideoMuteOff")]
		public JoinDataComplete VideoMuteOff = new JoinDataComplete(new JoinData { JoinNumber = 5, JoinSpan = 1 },
			new JoinMetadata { Description = "VideoMuteOn", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("VideoMuteOn")]
		public JoinDataComplete VideoMuteOn = new JoinDataComplete(new JoinData { JoinNumber = 6, JoinSpan = 1 },
			new JoinMetadata { Description = "VideoMuteOff", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("VideoMuteToggle")]
		public JoinDataComplete VideoMuteToggle = new JoinDataComplete(new JoinData { JoinNumber = 7, JoinSpan = 1 },
			new JoinMetadata { Description = "VideoMuteToggle", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("VShiftPlus")]
		public JoinDataComplete VShiftPlus = new JoinDataComplete(new JoinData { JoinNumber = 40, JoinSpan = 1 },
			new JoinMetadata { Description = "VShiftPlus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("VShiftMinus")]
		public JoinDataComplete VShiftMinus = new JoinDataComplete(new JoinData { JoinNumber = 41, JoinSpan = 1 },
			new JoinMetadata { Description = "VShiftMinus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("HShiftPlus")]
		public JoinDataComplete HShiftPlus = new JoinDataComplete(new JoinData { JoinNumber = 42, JoinSpan = 1 },
			new JoinMetadata { Description = "HShiftPlus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("HShiftMinus")]
		public JoinDataComplete HShiftMinus = new JoinDataComplete(new JoinData { JoinNumber = 43, JoinSpan = 1 },
			new JoinMetadata { Description = "HShiftMinus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("FocusPlus")]
		public JoinDataComplete FocusPlus = new JoinDataComplete(new JoinData { JoinNumber = 44, JoinSpan = 1 },
			new JoinMetadata { Description = "FocusPlus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("FocusMinus")]
		public JoinDataComplete FocusMinus = new JoinDataComplete(new JoinData { JoinNumber = 45, JoinSpan = 1 },
			new JoinMetadata { Description = "FocusMinus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ZoomPlus")]
		public JoinDataComplete ZoomPlus = new JoinDataComplete(new JoinData { JoinNumber = 46, JoinSpan = 1 },
			new JoinMetadata { Description = "ZoomPlus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ZoomMinus")]
		public JoinDataComplete ZoomMinus = new JoinDataComplete(new JoinData { JoinNumber = 47, JoinSpan = 1 },
			new JoinMetadata { Description = "ZoomMinus", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });

		#endregion


		#region Analog

		// TODO [ ] Add analog joins below plugin being developed

		/// <summary>
		/// Plugin socket status join map
		/// </summary>
		/// <remarks>
		/// Typically used with socket based communications.  Reports the socket state to SiMPL as an analog value.
		/// </remarks>
		/// <see cref="Crestron.SimplSharp.CrestronSockets.SocketStatus"/>
		[JoinName("SocketStatus")]
		public JoinDataComplete SocketStatus = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Socket SocketStatus",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		/// <summary>
		/// Plugin monitor status join map
		/// </summary>
		/// <remarks>
		/// Typically used with comms monitor to report plugin monitor state for system status page and Fusion monitor state.
		/// </remarks>
		/// <see cref="PepperDash.Essentials.Core.MonitorStatus"/>
		[JoinName("MonitorStatus")]
		public JoinDataComplete MonitorStatus = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 2,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Monitor Status",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

        /// <summary>
        /// Report Lamp Hours
        /// </summary>
        [JoinName("LampHours")]
        public JoinDataComplete LampHours = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Lamp Hours",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.AnalogSerial
            });

		#endregion


		#region Serial

		// TODO [ ] Add serial joins below plugin being developed

		/// <summary>
		/// Plugin device name
		/// </summary>
		/// <remarks>
		/// Reports the plugin name, as read from the configuration file, to SiMPL as a string value.
		/// </remarks>
		[JoinName("DeviceName")]
		public JoinDataComplete DeviceName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Device Name",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

        /// <summary>
        /// Plugin device name
        /// </summary>
        /// <remarks>
        /// Reports the plugin name, as read from the configuration file, to SiMPL as a string value.
        /// </remarks>


		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
		public NecCommonAsciiDevicePluginBridgeJoinMap(uint joinStart)
			: base(joinStart, typeof(NecCommonAsciiDevicePluginBridgeJoinMap))
		{
		}
	}
}
