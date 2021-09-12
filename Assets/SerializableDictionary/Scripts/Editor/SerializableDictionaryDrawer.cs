using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

[CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
public class SerializableDictionaryDrawer: PropertyDrawer {

	bool foldout=true;
	List<float> heights = new List<float>();

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
		UnityEngine.Object targetObject = property.serializedObject.targetObject;

		position.height = 18;
		foldout = EditorGUI.Foldout(position,foldout,label);
		if (foldout){
			EditorGUI.indentLevel++;
			Rect indexRect = new Rect(position);
			Rect keysRect = new Rect(indexRect);
			Rect valuesRect = new Rect(keysRect);
			indexRect.y=keysRect.y = valuesRect.y = position.y+18;
			indexRect.width=40;
			keysRect.x=indexRect.xMax;
			keysRect.width=(keysRect.width-indexRect.xMax)*0.45f;
			valuesRect.x=keysRect.xMax;
			valuesRect.width=keysRect.width;
			EditorGUI.LabelField(keysRect,"Key");
			EditorGUI.LabelField(valuesRect,"Value");
			heights.Clear();
			heights.Add(20);

			for (int i = 0; i < property.FindPropertyRelative("keys").arraySize; i++){
				keysRect.y+=heights[i];
				valuesRect.y+=heights[i];
				indexRect.y+=heights[i];
				EditorGUI.LabelField(indexRect,i.ToString());
				Rect buttonRect = new Rect(valuesRect.xMax+10,valuesRect.y,18,valuesRect.height);
				EditorGUI.PropertyField(keysRect,property.FindPropertyRelative("keys").GetArrayElementAtIndex(i),new GUIContent(),true);
				EditorGUI.PropertyField(valuesRect, property.FindPropertyRelative("values").GetArrayElementAtIndex(i),new GUIContent(),true);
				if (GUI.Button(buttonRect,"X"))
					fieldInfo.GetValue(targetObject).GetType().GetMethod("RemoveAt").Invoke(fieldInfo.GetValue(targetObject),new object[]{i});
				heights.Add(Mathf.Max(EditorGUI.GetPropertyHeight(property.FindPropertyRelative("keys").GetArrayElementAtIndex(i),new GUIContent(),true),
				                       EditorGUI.GetPropertyHeight(property.FindPropertyRelative("values").GetArrayElementAtIndex(i),new GUIContent(),true)));
			}
			EditorGUI.indentLevel--;

			Rect addButtonRect = new Rect(position);
			addButtonRect.y = indexRect.y+2+heights[heights.Count-1];
			addButtonRect.height = 18;
			if (GUI.Button(addButtonRect,"Add"))
				fieldInfo.GetValue(targetObject).GetType().GetMethod("Add",new Type[]{}).Invoke(fieldInfo.GetValue(targetObject),new object[]{});
			fieldInfo.GetValue(targetObject).GetType().GetMethod("Update",BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fieldInfo.GetValue(targetObject),new object[]{});
		}
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label){
		float extraHeight = 22;
		foreach (float f in heights)
			extraHeight+=f;
		return base.GetPropertyHeight(property, label)+(foldout?extraHeight:0);
	}
}
