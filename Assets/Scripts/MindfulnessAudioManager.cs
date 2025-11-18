using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Tutorials.Core.Editor;
using UnityEngine;

public class MindfulnessAudioManager : MonoBehaviour
{
    [SerializeField] private List<AudioClip> audioClips;
    private AudioSource audioSource;
    [SerializeField] private float timeBetweenClips = 10.0f;

    private void Start()
    {
        if (audioSource != null)
            audioSource = GetComponent<AudioSource>();
        else
        {
            audioSource = this.gameObject.AddComponent<AudioSource>();
        }

    }

    public void StartSpeech()
    {
        StartCoroutine(StartSpeechCoroutine());
    }

    public IEnumerator StartSpeechCoroutine()
    {
        foreach (AudioClip clip in audioClips) {
            
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitForSeconds(clip.length);
            yield return new WaitForSeconds(timeBetweenClips);
        }
    }
}
