using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomPropertyDrawer(typeof(StatDictonary))]
public class StatDictionaryDrawer : SerializableDictionaryDrawer {

	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
	{
		base.OnGUI (position, property, label);
	}
}

[CustomPropertyDrawer(typeof(VitalDictionary))]
public class VitalDictionaryDrawer : SerializableDictionaryDrawer {
	
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
	{
		base.OnGUI (position, property, label);
	}
}
