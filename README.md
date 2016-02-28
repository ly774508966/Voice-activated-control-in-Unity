## Voice activated commands in Unity using C#

Implement voice activated player control and/or in-game events to any Unity project in a C# script using the Api.ai SDK and Newtonsoft Json.NET framework. This specific example uses Unity's "2d platformer" example project and uses voice activated commands to jump. To test, simply install and open the 2d platformer sample Unity project and replace the PlayerControl.cs file with the one available here. Press the "z" key on your keyboard before speaking, say "jump" into the microphone, and press the "x" key after finishing.

## Code Example

	void HandleOnResult(object sender, AIResponseEventArgs e)
	{
		var aiResponse = e.Response;
		if (aiResponse != null) {
			var outText = JsonConvert.SerializeObject (aiResponse, jsonSettings);
			Debug.Log (outText);
			string command = "jump";
			if (outText.Contains(command)) {
				speechInput = true;
			}
		} else {
			Debug.LogError("Response is null");
		}
	}

## Motivation

Virtual Reality is an ever-growing technology. Voice activated control is to become a necessity alongside this in order to achieve a greater level of immersion.

## Installation

Download the Api.ai Unity SDK plugin bundle from the Unity Asset store to your project's Assets folder, available [here](https://www.assetstore.unity3d.com/en/#!/content/31498)

Ensure that Json.NET is avaialble in your Unity project. If not, download from [here](http://www.newtonsoft.com/json) and add it to your assets folder.

Install the 2d platformer sample Unity project [here](https://www.assetstore.unity3d.com/en/#!/content/11228)

## API Reference

Much of this project was not possible without the help of existing documentation. More information and code examples on how to get Api.ai working with Unity can be found [here](https://github.com/api-ai/api-ai-unity)