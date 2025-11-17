using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeAudio : MonoBehaviour
{
    [SerializeField] private List<AudioSource> audioSources;

    [SerializeField] private float minDelay = 7f;
    [SerializeField] private float maxDelay = 15f;


    private void Start()
    {
        foreach (AudioSource source in audioSources) {
            StartCoroutine(StartSoundRandomly(source));
        }
    }


    public IEnumerator StartSoundRandomly(AudioSource audio)
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay,maxDelay));
            audio.volume = Random.Range(0.1f,1.0f);
            audio.Play();
        }
    }
}
