using UnityEngine;

public enum GenerationType
{
    Model,
    Audio
}

public class GeneratedObjectInfo : MonoBehaviour
{
    public GenerationType type;
    public string fileName;
    public bool isLooping;
}