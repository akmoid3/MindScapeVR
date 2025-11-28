using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeAudio : MonoBehaviour
{
    [SerializeField] private float minDelay = 15f;
    [SerializeField] private float maxDelay = 30f;
    [SerializeField] private AudioSource source;
    private MeshRenderer objectMeshRenderer;
    private Collider objectCollider;
    private Coroutine soundRoutine;



    private void Start()
    {
        source = GetComponent<AudioSource>();

        if (objectMeshRenderer == null)
            objectMeshRenderer = GetComponent<MeshRenderer>();

        if (objectCollider == null)
            objectCollider = GetComponent<Collider>();

    }

    public void EnableMeshAndCollider(bool isEnabled)
    {
        if (objectMeshRenderer)
        {
            objectMeshRenderer.enabled = isEnabled;
        }
        else
        {
            objectCollider = GetComponent<Collider>();
            objectMeshRenderer.enabled = isEnabled;
        }

        if (objectCollider)
            objectCollider.enabled = isEnabled;
        else
        {
            objectMeshRenderer = GetComponent<MeshRenderer>();
            objectCollider.enabled = isEnabled;
        }
    }
    public void StartSoundRandomly()
    {
        if (source != null && soundRoutine == null)
            soundRoutine = StartCoroutine(StartSoundRandomlyCoroutine());
    }

    public IEnumerator StartSoundRandomlyCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay,maxDelay));
            source.volume = Random.Range(0.1f,1.0f);
            source.Play();
        }
    }

    public void StopRandomSound()
    {
        if (soundRoutine != null)
        {
            StopCoroutine(soundRoutine);
            soundRoutine = null;
        }
    }



}
