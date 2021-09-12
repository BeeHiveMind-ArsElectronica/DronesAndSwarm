using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {
	
	public StatDictonary stats = new StatDictonary(){
		{"Attack",100},
		{"Defense",80},
		{"Wisdom",50},
	};

	public VitalDictionary vitals = new VitalDictionary(){
		{"Health", new Vital(2000,2000)},
		{"Mana", new Vital(100,50)}
	};
	
	void OnGUI(){
		float lastVital = 0;
		for (int i = 0; i < vitals.Count; i++){
			lastVital = 10+20*i;
			GUI.Label(new Rect(10,lastVital,100,20),vitals[i].Key+":");
			GUI.Label(new Rect(110,lastVital,100,20),vitals[i].Value.current.ToString()+"/"+vitals[i].Value.max.ToString());
			if (GUI.RepeatButton(new Rect(210,lastVital,20,20),"-"))
				vitals[i].Value.current-=1;
			if (GUI.RepeatButton(new Rect(230,lastVital,20,20),"+"))
				vitals[i].Value.current+=1;
		}
		if (GUI.Button(new Rect(10,lastVital+20,200,20),"Add Vital"))
			vitals.Add("Stamina",new Vital(200,170));

		float lastStat = lastVital+40;
		for (int i = 0; i < stats.Count; i++){
			lastStat+=20;
			GUI.Label(new Rect(10,lastStat,100,20),stats[i].Key+":");
			GUI.Label(new Rect(110,lastStat,100,20),stats[i].Value.ToString());
			if (GUI.Button(new Rect(210,lastStat,20,20),"X"))
				stats.RemoveAt(i);
		} 
		if (GUI.Button(new Rect(10,lastStat+20,200,20),"Add Stat"))
			stats.Add("Agility",60);
	}
}

[System.Serializable]
public class StatDictonary:SerializableDictionary<string,float>{}

[System.Serializable]
public class VitalDictionary:SerializableDictionary<string,Vital>{}

[System.Serializable]
public class Vital{
	public float max;
	public float current;

	public Vital(float max, float current){
		this.max = max;
		this.current = current;
	}
}