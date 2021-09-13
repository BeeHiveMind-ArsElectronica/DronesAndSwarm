using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    [SerializeField] [Range(0f,1f)] private float volume;
    [SerializeField] [Range(0f,3f)] private float pitch;

    private AudioSource audio_source;
    [SerializeField] private AudioClip clip;

    public VNectSwarmOSWrapper wrapper;

    void Start()
    {

        audio_source = GetComponent<AudioSource>();
        audio_source.clip = clip;
        audio_source.Play();

        // Init volume
        volume = 0.5f;
        audio_source.volume = volume;

        // Init pitch
        pitch = 1f;
        audio_source.pitch = pitch;
    }

    void Update()
    {
        float[] _heightJoints = wrapper.GetYPercent();
        int _maxJoints = wrapper.GetNumMappedJoints();
        float _sum = 0.0f;

        for (int i = 0; i < _maxJoints; i++)
        {
            _sum = _sum + _heightJoints[i];
        }
        float average = _sum / _maxJoints;
        //Debug.Log(i + "Y: " + yPercent);

        volume = average * 10;
        audio_source.volume = volume;
        audio_source.pitch = pitch;

    }
}
