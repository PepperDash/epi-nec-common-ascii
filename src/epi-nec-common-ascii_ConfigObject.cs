using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace EpiNecCommonAscii
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being created
	/// </remarks>
	/// <example>
	/// "EssentialsPluginConfigObjectTemplate" renamed to "SamsungMdcConfig"
	/// <code>
	/// {
	///		"devices": [
	///			{
	///				"key": "essentialsPluginKey",
	///				"name": "Essentials Plugin Name",
	///				"type": "essentialsPluginTypeName",
	///				"group": "pluginDevices",
	///				"properties": {
	///					"control": {
	///						"method": "PepperDash.Core.eControlMethod",
	///						"controlPortDevKey": "examplePortDevKey",
	///						"controlPortNumber": 1,
	///						"comParams": {
	///							"baudRate": 9600,
	///							"dataBits": 8,
	///							"stopBits": 1,
	///							"parity": "None",
	///							"protocol": "RS232",
	///							"hardwareHandshake": "None",
	///							"softwareHandshake": "None"
	///						},
	///						"tcpSshProperties": {
	///							"address": "172.22.0.101",
	///							"port": 23,
	///							"username": "admin",
	///							"password": "password",
	///							"autoReconnect": true,
	///							"autoReconnectIntervalMs": 10000
	///						}
	///					},
	///					"pollTimeMs": 30000,
	///					"warningTimeoutMs": 180000,
	///					"errorTimeoutMs": 300000,
	///					"pluginCollection": {
	///						"item1": {
	///							"name": "Item 1",
	///							"value": 1
	///						}
	///						"item2": {
	///							"name": "Item 2",,
	///							"value": 2
	///						}
	///					}
	///				}
	///			}
	///		]
	/// }
	/// </code>
	/// </example>
	[ConfigSnippet("{\"devices\":[{\"key\":\"essentialsPluginKey\",\"name\":\"Essentials Plugin Name\",\"type\":\"essentialsPluginTypeName\",\"group\":\"pluginDevices\",\"properties\":{\"control\":{\"method\":\"PepperDash.Core.eControlMethod\",\"controlPortDevKey\":\"exampleControlPortDevKey\",\"controlPortNumber\":1,\"comParams\":{\"baudRate\":9600,\"dataBits\":8,\"stopBits\":1,\"parity\":\"None\",\"protocol\":\"RS232\",\"hardwareHandshake\":\"None\",\"softwareHandshake\":\"None\"},\"tcpSshProperties\":{\"address\":\"172.22.0.101\",\"port\":22,\"username\":\"admin\",\"password\":\"password\",\"autoReconnect\":true,\"autoReconnectIntervalMs\":10000}},\"pollTimeMs\":30000,\"warningTimeoutMs\":180000,\"errorTimeoutMs\":300000,\"pluginCollection\":{\"item1\":{\"name\":\"Item 1\",\"value\":1},\"item2\":{\"name\":\"Item 2\",\"value\":2}}}}]}")]
	public class NecCommonAsciiDeviceConfigObject : DeviceConfig
	{
		/// <summary>
		/// JSON control object
		/// </summary>
		/// <remarks>
		/// Typically this object is not required, but in some instances it may be needed.  For example, when building a 
		/// plugin that is using Telnet (TCP/IP) communications and requires login, the device will need to handle the login.
		/// In order to do so, you will need the username and password in the "tcpSshProperties" object.
		/// </remarks>
		/// <example>
		/// <code>
		///	"control": {
        ///		"method": "tcpIp",
		///		"controlPortDevKey": "processor",
		///		"controlPortNumber": 1,
		///		"comParams": {
		///			"baudRate": 9600,
		///			"dataBits": 8,
		///			"stopBits": 1,
		///			"parity": "None",
		///			"protocol": "RS232",
		///			"hardwareHandshake": "None",
		///			"softwareHandshake": "None"
		///		},
		///		"tcpSshProperties": {
		///			"address": "172.22.0.101",
		///			"port": 23,
		///			"username": "admin",
		///			"password": "password",
		///			"autoReconnect": true,
		///			"autoReconnectIntervalMs": 10000
		///		}
		///	}				
		/// </code>
		/// </example>		
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		/// <summary>
		/// Serializes the poll time value
		/// </summary>
		/// <remarks>
		/// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
		/// </remarks>
		/// <value>
		/// PollTimeMs property gets/sets the value as a long
		/// </value>
		/// <example>
		/// <code>
		/// "properties": {
		///		"polltimeMs": 30000
		/// }
		/// </code>
		/// </example>
		[JsonProperty("pollTimeMs")]
		public long PollTimeMs { get; set; }

		/// <summary>
		/// Serializes the warning timeout value
		/// </summary>
		/// <remarks>
		/// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
		/// </remarks>
		/// <value>
		/// WarningTimeoutMs property gets/sets the value as a long
		/// </value>
		/// <example>
		/// <code>
		/// "properties": {
		///		"warningTimeoutMs": 180000
		/// }
		/// </code>
		/// </example>
		[JsonProperty("warningTimeoutMs")]
		public long WarningTimeoutMs { get; set; }

		/// <summary>
		/// Serializes the error timeout value
		/// </summary>
		/// /// <remarks>
		/// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
		/// </remarks>
		/// <value>
		/// ErrorTimeoutMs property gets/sets the value as a long
		/// </value>
		/// <example>
		/// <code>
		/// "properties": {
		///		"errorTimeoutMs": 300000
		/// }
		/// </code>
		/// </example>
		[JsonProperty("errorTimeoutMs")]
		public long ErrorTimeoutMs { get; set; }

        /// <summary>
        /// Serializes the warming time value
        /// </summary>
        /// /// <remarks>
        /// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <value>
        /// WarmingTimeMs property gets/sets the value as a uint
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"warmingTimeMs": 3000
        /// }
        /// </code>
        /// </example>
        [JsonProperty("warmingTimeMs")]
        public uint WarmingTimeMs { get; set; }

        /// <summary>
        /// Serializes the cooling time value
        /// </summary>
        /// /// <remarks>
        /// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <value>
        /// CoolingTimeMs property gets/sets the value as a uint
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"coolingTimeMs": 3000
        /// }
        /// </code>
        /// </example>
        [JsonProperty("coolingTimeMs")]
        public uint CoolingTimeMs { get; set; }

		/// <summary>
		/// Inputs the device exposes, as routing ports and selectable items
		/// </summary>
		/// <remarks>
		/// Known keys are hdmiIn1, hdmiIn2, rgbIn1 and hdbaseT1. Each entry is either a key or an object with a key and
		/// a display name. When missing or empty, hdmiIn1, hdmiIn2 and rgbIn1 are exposed. Unknown keys are logged and ignored.
		/// </remarks>
		/// <example>
		/// <code>
		/// "properties": {
		///		"inputs": ["hdmiIn1", { "key": "hdmiIn2", "name": "Laptop" }]
		/// }
		/// </code>
		/// </example>
		[JsonProperty("inputs")]
		public List<NecInputConfig> Inputs { get; set; }

		/// <summary>
		/// Whether the poll queries the shutter (video mute) state
		/// </summary>
		/// <remarks>
		/// Defaults to true. Set false for displays with no shutter, which answer the query with
		/// "error:command". The shutter is never polled while power is off.
		/// </remarks>
		/// <example>
		/// <code>
		/// "properties": {
		///		"pollShutter": false
		/// }
		/// </code>
		/// </example>
		[JsonProperty("pollShutter")]
		public bool PollShutter { get; set; } = true;


		/// <summary>
		/// Example dictionary of objects
		/// </summary>
		/// <remarks>
		/// This is an example collection configuration object.  This should be modified or deleted as needed for the plugin being built.
		/// </remarks>
		/// <example>
		/// <code>
		/// "properties": {
		///		"presets": {
		///			"preset1": {
		///				"enabled": true,
		///				"name": "Preset 1"
		///			}
		///		}
		/// }
		/// </code>
		/// </example>
		/// <example>
		/// <code>
		/// "properties": {
		///		"inputNames": {
		///			"input1": "Input 1",
		///			"input2": "Input 2"
		///		}
		/// }
		/// </code>
		/// </example>
		[JsonProperty("pluginCollection")]
		public Dictionary<uint, EssentialsPluginConfigObjectDictionaryTemplate> PluginCollection { get; set; }

		/// <summary>
		/// Constuctor
		/// </summary>
		/// <remarks>
		/// If using a collection you must instantiate the collection in the constructor
		/// to avoid exceptions when reading the configuration file 
		/// </remarks>
		public NecCommonAsciiDeviceConfigObject()
		{
			PluginCollection = new Dictionary<uint, EssentialsPluginConfigObjectDictionaryTemplate>();
		}


	}

	/// <summary>
	/// An input to expose, written in config as a key string or as { "key": ..., "name": ... }
	/// </summary>
	[JsonConverter(typeof(NecInputConfigConverter))]
	public class NecInputConfig
	{
		/// <summary>
		/// Input key: hdmiIn1, hdmiIn2 or rgbIn1
		/// </summary>
		[JsonProperty("key")]
		public string Key { get; set; }

		/// <summary>
		/// Optional display name; the default label is used when empty
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; }
	}

	/// <summary>
	/// Reads a <see cref="NecInputConfig"/> from either a key string or an object
	/// </summary>
	public class NecInputConfigConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType) => objectType == typeof(NecInputConfig);

		public override bool CanWrite => false;

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;

			if (reader.TokenType == JsonToken.String)
				return new NecInputConfig { Key = (string)reader.Value };

			var obj = JObject.Load(reader);
			return new NecInputConfig
			{
				Key = (string)obj["key"],
				Name = (string)obj["name"]
			};
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Example plugin configuration dictionary object
	/// </summary>
	/// <remarks>
	/// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
	/// </remarks>
	/// <example>
	/// <code>
	/// "properties": {
	///		"dictionary": {
	///			"item1": {
	///				"name": "Item 1 Name",
	///				"value": "Item 1 Value"
	///			}
	///		}
	/// }
	/// </code>
	/// </example>
	public class EssentialsPluginConfigObjectDictionaryTemplate
	{
		/// <summary>
		/// Serializes collection name property
		/// </summary>
		/// <remarks>
		/// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
		/// </remarks>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Serializes collection value property
		/// </summary>
		/// <remarks>
		/// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
		/// </remarks>
		[JsonProperty("value")]
		public uint Value { get; set; }
	}
}