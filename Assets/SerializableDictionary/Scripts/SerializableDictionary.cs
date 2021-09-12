using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SerializableDictionary<TKey,TValue>:IEnumerable {

	//==============VARS==============
	[SerializeField]
	List<TKey> keys;
	[SerializeField]
	List<TValue> values;

	//===========PROPERTIES===========
	public List<TKey> Keys{
		get{return keys;}
		private set{keys = value;}
	}
	public List<TValue> Values{
		get{return values;}
		private set{values = value;}
	}
	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	public TValue this[TKey key]{
		get{
			if (ContainsKey(key))
				return Values[Keys.IndexOf(key)];
			else
				throw new System.Collections.Generic.KeyNotFoundException("Key "+key.ToString()+" not found");
		}
		set{Values[Keys.IndexOf(key)] = value;}
	}
	/// <summary>
	/// Gets the KeyValuePair<TKey,TValue> associated with the specified index
	/// </summary>
	public KeyValuePair<TKey, TValue> this[int index]{
		get{
			if (index<Count && index >=0)
				return new KeyValuePair<TKey, TValue>(Keys[index],Values[index]);
			else
				throw new System.IndexOutOfRangeException("The index " + index+ " is out of range");
		}
	}
	/// <summary>
	/// Returns the number of elements
	/// </summary>
	public int Count{
		get{return Keys.Count;}
	}

	//==========CONSTRUCTORS==========
	public SerializableDictionary(){
		Keys = new List<TKey>();
		Values = new List<TValue>();
	}
	public SerializableDictionary(params KeyValuePair<TKey, TValue>[] values):this(){
		foreach(KeyValuePair<TKey, TValue> value in values)
			Add(value);
	}

	//==========METHODS===============
	protected void Update(){
		do{
			bool duplicates = false;
			for (int i = 0; i < Keys.Count-1; i++){
				for (int j =i+1; j < Keys.Count; j++){
					if (Keys[i].Equals(Keys[j])){
						duplicates = true;
						Keys.RemoveAt(j);
						Values.RemoveAt(j);
					}
				} 
			}
			if (!duplicates)
				break;
		}while (true);
	} 
	/// <summary>
	/// Adds the specified key and value to the dictionary.
	/// </summary>
	public void Add(TKey key, TValue value){
		if (ContainsKey(key))
			throw new System.ArgumentException("An element with the same key already exists");
		Keys.Add(key);
		Values.Add(value);
	}
	/// <summary>
	/// Adds the specified KeyValuePair to the dictionary
	/// </summary>
	public void Add(KeyValuePair<TKey, TValue> value){
		Add(value.Key, value.Value);
	}
	public void Add(){
		Add(default(TKey), default(TValue));
	}
	/// <summary>
	/// Removes all keys and values from the Dictionary
	/// </summary>
	public void Clear(){
		Keys.Clear();
		Values.Clear();
	}
	/// <summary>
	/// Returns whether the dictionary contains the specified key
	/// </summary>
	public bool ContainsKey(TKey key){
		return Keys.Contains(key);
	}
	/// <summary>
	/// Returns whether the dictionary contains a specific value
	/// </summary>
	public bool ContainsValue(TValue value){
		return Values.Contains(value);
	}
	/// <summary>
	/// Removes the value with the specified key from the Dictionary (and the key too)
	/// </summary>
	public bool Remove(TKey key){
		if (!ContainsKey(key))
			return false;
		Values.RemoveAt(Keys.IndexOf(key));
		Keys.Remove(key);
		return true;
	}
	/// <summary>
	/// Removes the key and the value stored at the specified index
	/// </summary>
	public bool RemoveAt(int index){
		try{
			return Remove(this[index].Key);
		}catch(System.IndexOutOfRangeException e){
			throw e;
		}
	}
	/// <summary>
	/// Gets the value associated with the specified key.
	/// Returns whether the key could be found
	/// </summary>
	public bool TryGetValue(TKey key,out TValue value){
		if (!ContainsKey(key)){
			value = default(TValue);
			return false;
		}
		value = this[key];
		return true;
	}
	/// <summary>
	/// Returns a List<KeyValuePair<TKey,TValue>> with all the keys and values in the dictionary
	/// </summary>
	public List<KeyValuePair<TKey,TValue>> ToList(){
		List<KeyValuePair<TKey,TValue>> list = new List<KeyValuePair<TKey, TValue>>();
		for (int i = 0; i < Keys.Count; i++)
			list.Add(new KeyValuePair<TKey, TValue>(Keys[i],Values[i]));
		return list;
	}
	/// <summary>
	/// Returns an actual System.Collections.generic.Dictionary with the elements in this dictionary
	/// </summary>
	public Dictionary<TKey,TValue> ToDictionary(){
		Dictionary<TKey,TValue> dictionary = new Dictionary<TKey,TValue>();
		for (int i = 0; i < Keys.Count; i++)
			dictionary.Add(Keys[i],Values[i]);
		return dictionary;
	}
	public IEnumerator GetEnumerator(){
		return Keys.GetEnumerator();
	}
}