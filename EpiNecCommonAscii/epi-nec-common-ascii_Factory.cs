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
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		/// <remarks>
		/// Update the MinimumEssentialsFrameworkVersion & TypeNames as needed when creating a plugin
		/// </remarks>
		/// <example>
		/// Set the minimum Essentials Framework Version
		/// <code>
		///  MinimumEssentialsFrameworkVersion = "1.5.5";
		/// </code>
		/// In the constructor we initialize the list with the typenames that will build an instance of this device
		/// <code>
		/// TypeNames = new List<string>() { "SamsungMdc", "SamsungMdcDisplay" };
		/// </code>
		/// </example>
		public NecCommonAsciiDevicePluginFactory()
		{
			// Set the minimum Essentials Framework Version
			// TODO [ ] Update the Essentials minimum framework version which this plugin has been tested against
			MinimumEssentialsFrameworkVersion  = "1.6.5";

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
				Debug.Console(0, new string('*', 80));
				Debug.Console(0, new string('*', 80));
				Debug.Console(0, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);				
				
				var propertiesConfig = dc.Properties.ToObject<NecCommonAsciiDeviceConfigObject>();
				if (propertiesConfig == null)
				{
					Debug.Console(0, "[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
					return null;
				}
				
				// If using a communication method not supported in PepperDash.Core.eControlMethod reference the EXAMPLE below of pulling out control method properties
				// ** Update as needed for YOUR plugin **
				// get the plugin device control properties configuratin object & check for null
				var controlConfig = CommFactory.GetControlPropertiesConfig(dc);
				if (controlConfig == null)
				{
					Debug.Console(0, "[{0}] Factory: failed to read control config for {1}", dc.Key, dc.Name);
				}
				// TODO [ ] If using an unsupported PepperDash.Core.eControlMethod, you can selective pull property values out of the JSON control block with the examples below			
				else if(controlConfig.Method.ToString().Contains("http"))
				{
					
					var address = controlConfig.TcpSshProperties.Address;
					var port = controlConfig.TcpSshProperties.Port;
					Debug.Console(0, "[{0}] {1} will attempt to connect using: {2}:{3}", dc.Key, dc.Name, address, port);

					var username = controlConfig.TcpSshProperties.Username;
					var password = controlConfig.TcpSshProperties.Password;
					Debug.Console(1, "[{0}] {1} will attempt to use authorization credentials: {2}:{3}", dc.Key, dc.Name, username, password);

					// TODO [ ] Update with the proper constructor to instantiate the device using HTTPS
					throw new NotImplementedException();
				}

				// TODO [ ] If your device is using a PepperDash.Core.eControlMethod supported enum, the snippet below will support standard comm methods
				// build the plugin device comms (for all other comms methods) & check for null			
				var comms = CommFactory.CreateCommForDevice(dc);
                if (comms != null) return new NecCommonAsciiDevice(dc.Key, dc.Name, propertiesConfig, comms);
				Debug.Console(0, "[{0}] Factory: failed to create comm for {1}", dc.Key, dc.Name);
				return null;
			}
			catch (Exception ex)
			{
				Debug.Console(0, "[{0}] Factory BuildDevice Exception: {1}", dc.Key, ex);
				return null;
			}
		}
	}
}