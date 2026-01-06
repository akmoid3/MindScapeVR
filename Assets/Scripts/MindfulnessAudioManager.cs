using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MindfulnessAudioManager : MonoBehaviour
{
    [SerializeField] private List<AudioClip> audioClips;
    [SerializeField] private float timeBetweenClips = 40.0f;

    private AudioSource audioSource;
    private Coroutine speechCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void StartSpeech()
    {
        StopSpeech();

        speechCoroutine = StartCoroutine(StartSpeechCoroutine());
    }

    public void StopSpeech()
    {
        if (speechCoroutine != null)
        {
            StopCoroutine(speechCoroutine);
            speechCoroutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public IEnumerator StartSpeechCoroutine()
    {
        yield return new WaitForSeconds(5.0f);
        foreach (AudioClip clip in audioClips)
        {
            if (clip == null) continue;

            audioSource.clip = clip;
            audioSource.Play();

            yield return new WaitForSeconds(clip.length);

            yield return new WaitForSeconds(timeBetweenClips);
        }

        speechCoroutine = null;
    }
}