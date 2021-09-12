using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomPropertyDrawer(typeof(Vital))]
public class VitalDrawer : PropertyDrawer {

	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label){
		position.width*=0.5f;
		Rect maxRect = new Rect(position);
		EditorGUI.PrefixLabel(maxRect,new GUIContent("Max"));
		maxRect.x+=maxRect.width;
		property.FindPropertyRelative("max").floatValue = 
			EditorGUI.FloatField(maxRect,property.FindPropertyRelative("max").floatValue);
		Rect currentRect = new Rect(position);
		currentRect.y+=20;
		EditorGUI.PrefixLabel(currentRect,new GUIContent("Current"));
		currentRect.x+=currentRect.width;
		property.FindPropertyRelative("current").floatValue = 
			EditorGUI.FloatField(currentRect,property.FindPropertyRelative("current").floatValue);
	}

	public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
	{
		return base.GetPropertyHeight (property, label)+20;
	}
}
