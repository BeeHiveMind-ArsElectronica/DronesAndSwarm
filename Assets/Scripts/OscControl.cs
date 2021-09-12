using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using UnityOSC;

public class OscControl : MonoBehaviour {
	
	private Dictionary<string, ServerLog> servers;

	private int m_lastPacketIndex = -1;
	private bool m_newPacket = false;

	public string Ip1;
	public int Port1;
	public string Ip2;
	public int Port2;

	private static OscControl instance;

	// Static singleton property
	public static OscControl Instance
	{
		// Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
		// otherwise we assign instance to a new component and return that
		get { return instance ?? (instance = GameObject.Find("OscControl").GetComponent<OscControl>()); }

	}
	
	// Script initialization
	void Start() {	
		OSCHandler.Instance.Init(); //init OSC
		servers = new Dictionary<string, ServerLog>();

		OSCHandler.Instance.CreateServer("oscs01", 57120);
		//servers.Add("oscs01", new ServerLog());
		// OSCHandler.Instance.Servers["oscs01"].server.PacketReceivedEvent += OnPacketReceived;

		OSCHandler.Instance.CreateClient("vis", IPAddress.Parse(Ip1),  Port1);
	}

	public void SendState(string address, string state)
	{
		
		OSCHandler.Instance.SendMessageToClient("vis", address, state);
	}

	public static float ParseEnglishFloat(object o)
	{
		return float.Parse(o.ToString(), System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture);
	}

	// NOTE: The received messages at each server are updated here
    // Hence, this update depends on your application architecture
    // How many frames per second or Update() calls per frame?
	void Update() {
		
		OSCHandler.Instance.UpdateLogs();
		servers = OSCHandler.Instance.Servers;
		
	    foreach( KeyValuePair<string, ServerLog> item in servers )
		{
			// If we have received at least one packet,
			// show the last received from the log in the Debug console
			if(item.Value.log.Count > 0) 
			{
				int lastPacketIndex = item.Value.packets.Count - 1;
				if (m_lastPacketIndex != lastPacketIndex)
				{
					m_lastPacketIndex = lastPacketIndex;
					m_newPacket = true;
				}
				else
				{
					m_newPacket = false;
				}

				if (m_newPacket)
				{
					Debug.Log("new packet " + item.Value.packets[lastPacketIndex].Address + " : " + item.Value.packets[lastPacketIndex].Data[0].ToString());
				}
			}
	    }
	}

	void OnApplicationQuit()
	{
		
	}
}