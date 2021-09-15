/*
 * Copyright (c) 2015 Allan Pichardo
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *  http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using System;
using System.Collections.Generic;




public class ChangeColorBeatDetection : MonoBehaviour
{
	/*<h2>Usage</h2>
	Add the AudioProcessor script to your <b>Main Camera</b> object and 
	adjust the <b>threshold</b> parameter to change the sensitivity. 
	Then set a callback delegate on the audio processor's <b>onBeat</b> or <b>onSpectrum</b> events.*/

	[HideInInspector] public Renderer colorDroneRender;
	[SerializeField] [Range(0f, 1f)] private float color_speed;
	[SerializeField] [Range(0f, 0.1f)] private float decadence;
	private Color droneColor;
	[SerializeField] private List<Color> colorList = new List<Color>();
	private int index = 0;
	private float t;

	private float alpha = 0.5f;


	AudioManager m_audio;

	void Start ()
	{
		//Select the instance of AudioProcessor and pass a reference
		//to this object
		m_audio = FindObjectOfType<AudioManager> ();
		AudioProcessor processor = FindObjectOfType<AudioProcessor> ();
		processor.onBeat.AddListener (onOnbeatDetected);
		processor.onSpectrum.AddListener (onSpectrum);

		//BeeHiveMind
		colorDroneRender = GetComponent<Renderer>();
		droneColor = new Color(colorDroneRender.material.color.r, colorDroneRender.material.color.g, colorDroneRender.material.color.b, colorDroneRender.material.color.a);
	}

	//this event will be called every time a beat is detected.
	//Change the threshold parameter in the inspector
	//to adjust the sensitivity
	void onOnbeatDetected ()
	{
		Debug.Log ("Beat!!!");
		alpha = 1f;

	}

	//This event will be called every frame while music is playing
	void onSpectrum(float[] spectrum)
	{
		//The spectrum is logarithmically averaged
		//to 12 bands

		for (int i = 0; i < spectrum.Length; ++i)
		{
			Vector3 start = new Vector3(i, 0, 0);
			Vector3 end = new Vector3(i, spectrum[i], 0);
			Debug.DrawLine(start, end);
		}



		//// BeeHiveMind

		//droneColor = Color.Lerp(droneColor, colorList[index],color_speed);

		//t = Mathf.Lerp(t, 1f, color_speed * Time.deltaTime);
		//if(t > 9f)
  //      {
		//	t = 0f;
		//	index++;
		//	index = (index >= colorList.Count) ? 0 : index;
		//}
		
		//colorDroneRender.material.color = droneColor;

	}
    private void Update()
    {
		// BeeHiveMind

		droneColor = Color.Lerp(droneColor, colorList[index], color_speed * Time.deltaTime);

		t = Mathf.Lerp(t, 1f, color_speed * Time.deltaTime);
		if (t > .9f)
		{
			t = 0f;
			index++;
			index = (index >= colorList.Count) ? 0 : index;
		}

		alpha -= decadence;
		if (alpha <= 0.5f) alpha = 0.5f;
		colorDroneRender.material.color = new Color(droneColor.r, droneColor.g, droneColor.b, alpha);

	}

	public Color GetDroneColor() { return droneColor; }
}
