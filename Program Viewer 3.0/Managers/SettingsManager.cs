using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ProgramViewer3.Managers
{
	class SettingsManager
	{
		private Dictionary<string, dynamic> settingsJson;
		private readonly Dictionary<string, SettingField> settingFields = new Dictionary<string, SettingField>();
		private readonly Dictionary<string, object> settingValues = new Dictionary<string, object>();

		private static string SettingFilename = Path.Combine(ItemManager.ApplicationPath, "Settings.json");

		public void Initialize()
		{
			CacheManager.InitializeJSONFile(SettingFilename);
			settingsJson = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(File.ReadAllText(SettingFilename));

			InitFieldFromJson("RedirectMessageLogging", true);
			InitFieldFromJson("LastUsedTheme", ThemeManager.DefaultThemeName);
			InitFieldFromJson("", ThemeManager.DefaultThemeName);
		}

		public void CloseManager()
		{
			File.WriteAllText(SettingFilename, JsonConvert.SerializeObject(settingValues, Formatting.Indented));
		}

		public T GetSettingValue<T>(string parameterName)
		{
			if (settingFields.ContainsKey(parameterName))
				return (T)settingFields[parameterName].Value;
			else
				throw new KeyNotFoundException($"Key '{parameterName}' is not presented in settings dictionary");
		}

		public void SetSettingValue<T>(string parameterName, T value)
		{
			if (settingFields.ContainsKey(parameterName))
			{
				var temp = settingFields[parameterName];
				temp.Value = (object)value;
				settingFields[parameterName] = temp;
				settingValues[parameterName] = value;
			}
			else
				throw new KeyNotFoundException($"Key '{parameterName}' is not presented in settings dictionary");
		}

		private void InitFieldFromJson(string name, object defalutValue)
		{
			SettingField field = new SettingField(name, defalutValue);
			if (settingsJson.ContainsKey(name))
			{
				var value = settingsJson[name];
				if (value.GetType() == defalutValue.GetType())
				{
					field.Value = (object)value;
				}
				else
				{
					LogManager.Write($"SettingField '{name}' value type '{value.GetType()}' must match default value type '{defalutValue.GetType()}'");
				}
			}
			settingFields.Add(name, field);
			settingValues.Add(name, field.Value);
		}
	}

	public struct SettingField
	{
		public string Name;
		public object Value;
		public object DefalutValue;

		public SettingField(string name, object defalutValue)
		{
			Name = name;
			Value = defalutValue;
			DefalutValue = defalutValue;
		}
	}

}
