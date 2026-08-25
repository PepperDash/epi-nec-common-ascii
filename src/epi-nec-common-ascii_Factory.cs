using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.UI;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace EpiNecCommonAscii
{
	/// <summary>
	/// Plugin factory for devices that require communications using IBasicCommunications or custom communication methods
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed and update the factory as needed.
	/// If this class is not used, delete the class and delete the associated EssentialsPluginDeviceTemplate.cs file from the solution
	/// </remarks>
	/// <example>
	/// "EssentialsPluginFactoryTemplate" renamed to "SamsungMdcFactory"
	/// </example>
	public class NecCommonAsciiDevicePluginFactory : EssentialsPluginDeviceFactory<NecCommonAsciiDevice>
	{

		public NecCommonAsciiDevicePluginFactory()
		{
			// Set the minimum Essentials Framework Version
			MinimumEssentialsFrameworkVersion  = "3.0.0";

			// In the constructor we initialize the list with the typenames that will build an instance of this device
			// only include unique typenames, when the constructur is used all the typenames will be evaluated in lower case.
			// TODO [ ] Update the TypeNames for the plugin being developed
			TypeNames = new List<string>() { "NecCommonAscii", "nec common ascii", "nec ascii projector"};
		}

		/// <summary>
		/// Builds and returns an instance of EssentialsPluginDeviceTemplate
		/// </summary>
		/// <param name="dc">device configuration</param>
		/// <returns>plugin device or null</returns>
		/// <remarks>		
		/// The example provided below takes the device key, name, properties config and the comms device created.
		/// Modify the EssetnialsPlugingDeviceTemplate constructor as needed to meet the requirements of the plugin device.
		/// </remarks>
		/// <seealso cref="PepperDash.Core.eControlMethod"/>
		public override EssentialsDevice BuildDevice(DeviceConfig dc)
		{
			try
			{
				Debug.LogDebug(new string('*', 80));
				Debug.LogDebug(new string('*', 80));
				Debug.LogDebug("[{Key}] Factory Attempting to create new device from type: {Type}", dc.Key, dc.Type);				
				
				var propertiesConfig = dc.Properties.ToObject<NecCommonAsciiDeviceConfigObject>();
				if (propertiesConfig == null)
				{
					Debug.LogError("[{Key}] Factory: failed to read properties config for {Name}", dc.Key, dc.Name);
					return null;
				}
				
				// If using a communication method not supported in PepperDash.Core.eControlMethod reference the EXAMPLE below of pulling out control method properties
				// ** Update as needed for YOUR plugin **
				// get the plugin device control properties configuratin object & check for null
				var controlConfig = CommFactory.GetControlPropertiesConfig(dc);
				if (controlConfig == null)
				{
					Debug.LogDebug("[{Key}] Factory: failed to read control config for {Name}", dc.Key, dc.Name);
				}
				// TODO [ ] If using an unsupported PepperDash.Core.eControlMethod, you can selective pull property values out of the JSON control block with the examples below			
				else if(controlConfig.Method.ToString().Contains("http"))
				{
					
					var address = controlConfig.TcpSshProperties.Address;
					var port = controlConfig.TcpSshProperties.Port;
					Debug.LogDebug("[{Key}] {Name} will attempt to connect using: {Address}:{Port}", dc.Key, dc.Name, address, port);

					var username = controlConfig.TcpSshProperties.Username;
					// Password intentionally omitted from the log - avoid writing credentials to logs.
					Debug.LogDebug("[{Key}] {Name} will attempt to use authorization credentials for user: {Username}", dc.Key, dc.Name, username);

					// TODO [ ] Update with the proper constructor to instantiate the device using HTTPS
					throw new NotImplementedException();
				}

				// TODO [ ] If your device is using a PepperDash.Core.eControlMethod supported enum, the snippet below will support standard comm methods
				// build the plugin device comms (for all other comms methods) & check for null			
				var comms = CommFactory.CreateCommForDevice(dc);
                if (comms != null) return new NecCommonAsciiDevice(dc.Key, dc.Name, propertiesConfig, comms);
				Debug.LogError("[{Key}] Factory: failed to create comm for {Name}", dc.Key, dc.Name);
				return null;
			}
			catch (Exception ex)
			{
				Debug.LogError(ex, "[{Key}] Factory BuildDevice Exception", dc.Key);
				return null;
			}
		}
	}
}