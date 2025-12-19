using NUnit.Framework;
using UnityEngine;

public class GeneratedObjectInfoTests
{
    [Test]
    public void Defaults_AreExpected()
    {
        var go = new GameObject("GeneratedObject");
        var info = go.AddComponent<GeneratedObjectInfo>();

        // enum default is 0 => GenerationType.Model
        Assert.AreEqual(GenerationType.Model, info.type);

        // string default is null
        Assert.IsNull(info.fileName);

        // bool default is false
        Assert.IsFalse(info.isLooping);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void CanSetAndReadFields()
    {
        var go = new GameObject("GeneratedObject");
        var info = go.AddComponent<GeneratedObjectInfo>();

        info.type = GenerationType.Audio;
        info.fileName = "job_123";
        info.isLooping = true;

        Assert.AreEqual(GenerationType.Audio, info.type);
        Assert.AreEqual("job_123", info.fileName);
        Assert.IsTrue(info.isLooping);

        Object.DestroyImmediate(go);
    }
}