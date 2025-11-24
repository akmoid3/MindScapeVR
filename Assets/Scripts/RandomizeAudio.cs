using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeAudio : MonoBehaviour
{
    [SerializeField] private float minDelay = 15f;
    [SerializeField] private float maxDelay = 30f;
    [SerializeField] private AudioSource source;
    private MeshRenderer meshRenderer;
    private Collider collider;



    private void Start()
    {
        source = GetComponent<AudioSource>();
        if(source != null )
            StartCoroutine(StartSoundRandomly(source));

        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<MeshRenderer>();

        if (collider == null)
            collider = GetComponent<Collider>();

    }

    public void EnableMeshAndCollider(bool isEnabled)
    {
        if(meshRenderer)
            meshRenderer.enabled = isEnabled;
        if(collider)
            collider.enabled = isEnabled;
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
