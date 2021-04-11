using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text;

public class SaveCompatibility : MonoBehaviour
{

	public static string FixStr(string message, char c)
    {
		// To remove double quotes (passed as 'c') of the chip name
        StringBuilder aStr = new StringBuilder(message);
        for (int i = 0; i < aStr.Length; i++)
        {
        if (aStr[i] == c)
        {
            aStr.Remove(i, 1);
        }
        }
        return aStr.ToString();
    }


    class OutputPin
	{
		[JsonProperty("name")]
		public string name { get; set; }
		[JsonProperty("wireType")]
		public int wireType { get; set; }
	}

	class InputPin
    {
		[JsonProperty("name")]
        public string name { get; set; }
		[JsonProperty("parentChipIndex")]
        public int parentChipIndex { get; set; }
		[JsonProperty("parentChipOutputIndex")]
        public int parentChipOutputIndex { get; set; }
		[JsonProperty("isCylic")]
        public bool isCylic { get; set; }
		[JsonProperty("wireType")]
        public int wireType { get; set; }
    }

    public static dynamic FixSaveCompatibility(string chipSaveString) {
		

		dynamic lol = JsonConvert.DeserializeObject<dynamic>(chipSaveString);

		for (int i = 0; i < lol.savedComponentChips.Count; i++) {

			List<OutputPin> newValue = new List<OutputPin>();
			List<InputPin> newValue2 = new List<InputPin>();

			// Replace all 'outputPinNames' : [string] in save with 'outputPins' : [OutputPin]
			for (int j = 0; j < lol.savedComponentChips[i].outputPinNames.Count; j++) {
				newValue.Add(new OutputPin{
					name= lol.savedComponentChips[i].outputPinNames[j],
					wireType= 0
				});
			}
			lol.savedComponentChips[i].Property("outputPinNames").Remove();
			lol.savedComponentChips[i].outputPins = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(newValue));
			
			// Add to all 'inputPins' dictionary the property 'wireType' with a value of 0 (at version 0.25 buses did not exist so its imposible for the wire to be of other type)
			for (int j = 0; j < lol.savedComponentChips[i].inputPins.Count; j++) {
				newValue2.Add(new InputPin{
					name= lol.savedComponentChips[i].inputPins[j].name,
					parentChipIndex = lol.savedComponentChips[i].inputPins[j].parentChipIndex,
					parentChipOutputIndex = lol.savedComponentChips[i].inputPins[j].parentChipOutputIndex,
					isCylic = lol.savedComponentChips[i].inputPins[j].isCylic,
					wireType = 0

				});
			}
			lol.savedComponentChips[i].inputPins =  JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(newValue2));
		}

		// Update save file. Delete the old one a create one with the updated version 
		string savePath = SaveSystem.GetPathToSaveFile( FixStr(JsonConvert.SerializeObject(lol.name), (char)0x22));
		File.Delete(savePath);
		using (StreamWriter writer = new StreamWriter(savePath))
		{
			writer.Write(JsonConvert.SerializeObject(lol, Formatting.Indented));
			writer.Close();
		}

		return JsonConvert.SerializeObject(lol, Formatting.Indented);
	}
}
