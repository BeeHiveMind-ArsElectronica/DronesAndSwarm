using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    [SerializeField] [Range(0f,1f)] private float volume;
    [SerializeField] [Range(0f,3f)] public float pitch;

    private AudioSource audio_source;
    [SerializeField] private AudioClip clip;

    public VNectSwarmOSWrapper wrapper;

    public GameObject visualDebugVolume;
    public GameObject visualDebugPitch;

    [SerializeField] [Range(0, 10)] private float pitch_treshold = 10;
    [SerializeField] [Range(0, 10)] private float volume_threshold = 10;

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
        /*AverageVolumeJoints();*/
        LeftHandVolumeRightHandPitch();

        // Debug
        VisualDebug();
    }


    void AverageVolumeJoints()
    {
        float[] _heightJoints = wrapper.GetYPercent();
        int _maxJoints = _heightJoints.Length;
        float _sum = 0.0f;

        for (int i = 0; i < _maxJoints; i++)
        {
            _sum += _heightJoints[i];
        }
        float average = _sum / _maxJoints;

        volume = average * 10;
        Debug.Log("Volume: " + volume);

        audio_source.volume = volume;
        audio_source.pitch = pitch;
    }

    void LeftHandVolumeRightHandPitch()
    {
        float[] _heightJoints = wrapper.GetYPercent();
        volume = _heightJoints[0] * volume_threshold;
        pitch = _heightJoints[1] * pitch_treshold;

        if (volume >= 1) volume = 1;
        if (volume <= 0.2f) volume = 0.2f;
        audio_source.volume = volume;
        if (pitch >= 3) pitch = 3;
        if (pitch <= 1f) pitch = 1f;
        audio_source.pitch = pitch;
    }

    void VisualDebug()
    {
        visualDebugPitch.transform.localScale = new Vector3(visualDebugVolume.transform.localScale.x, volume, visualDebugVolume.transform.localScale.z);
        visualDebugVolume.transform.localScale = new Vector3(visualDebugVolume.transform.localScale.x, pitch, visualDebugVolume.transform.localScale.z);

    }
}
