using UnityEngine;
using GLTFast;
using System.Collections;
using System.Diagnostics; // Necessario per Stopwatch
using System.IO;

public class ModelLoader : MonoBehaviour
{
    public string modelPath = "Assets/models/Beach.glb";
    private GameObject sceneObj;

    void Start()
    {
        StartCoroutine(LoadModelWithTimer());
    }

    IEnumerator LoadModelWithTimer()
    {
        // 1. Verifica se il file esiste
        string fullPath = Path.Combine(Application.dataPath, "..", modelPath);
        if (!File.Exists(fullPath))
        {
            UnityEngine.Debug.LogError($"File non trovato al percorso: {fullPath}");
            yield break;
        }

        GameObject container = new GameObject("GLB_Container");
        GltfAsset gltfAsset = container.AddComponent<GltfAsset>();

        // 2. Avvio del timer
        Stopwatch timer = new Stopwatch();
        UnityEngine.Debug.Log("Inizio caricamento GLB...");
        timer.Start();

        // 3. Esecuzione del caricamento
        var task = gltfAsset.Load(fullPath);
        yield return new WaitUntil(() => task.IsCompleted);

        // 4. Stop del timer
        timer.Stop();

        if (task.Result)
        {
            // Calcolo del tempo in secondi e millisecondi
            float elapsedSeconds = timer.ElapsedMilliseconds / 1000f;
            UnityEngine.Debug.Log($"<color=green>Caricamento completato!</color>");
            UnityEngine.Debug.Log($"Tempo impiegato: <b>{elapsedSeconds:F2} secondi</b> ({timer.ElapsedMilliseconds} ms)");
            
            // Logica aggiuntiva
            sceneObj = container;
            // ApplyMaterialToScene(container); // Se hai questa funzione definita altrove
        }
        else
        {
            UnityEngine.Debug.LogError($"Errore nel caricamento del file GLB.");
            Destroy(container);
        }
    }
}