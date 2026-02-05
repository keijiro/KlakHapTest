using System.Collections;
using Klak.Hap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class JapanesePathPlaybackTest
{
    const string TestPath = "Tests/日本語/Test.mov";

    [UnityTest]
    public IEnumerator CanPlayHapInJapanesePath()
    {
        if (!Application.isPlaying)
        {
            Assert.Ignore("PlayMode only.");
            yield break;
        }

        var go = new GameObject("HapTestPlayer");
        try
        {
            var player = go.AddComponent<HapPlayer>();
            player.Open(TestPath, HapPlayer.PathMode.StreamingAssets);

            for (var i = 0; i < 5; i++)
            {
                yield return null;
                player.UpdateNow();
            }

            var pathInfo = $"Resolved path: {player.resolvedFilePath}";
            Assert.That(player.isValid, Is.True, pathInfo);
            Assert.That(player.frameCount, Is.GreaterThan(0), pathInfo);
            Assert.That(player.frameWidth, Is.GreaterThan(0), pathInfo);
            Assert.That(player.frameHeight, Is.GreaterThan(0), pathInfo);
            Assert.That(player.texture, Is.Not.Null, pathInfo);
            Assert.That(player.codecType, Is.Not.EqualTo(CodecType.Unsupported), pathInfo);
        }
        finally
        {
            Object.Destroy(go);
        }
    }
}
