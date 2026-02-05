using System.Collections;
using System.IO;
using Klak.Hap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TestCardsPlaybackTest
{
    const string HapMoviePath = "Tests/Hap/TestCards.mov";
    const string HapFramesDirName = "Tests/Hap";
    const string HapQMoviePath = "Tests/HapQ/TestCards.mov";
    const string HapQFramesDirName = "Tests/HapQ";

    const float MatchThreshold = 0.99f;
    const float ChannelTolerance = 0.08f;

    static readonly string[] FramePngs =
    {
        "000001.png",
        "000002.png",
        "000003.png",
        "000004.png",
        "000005.png"
    };

    [UnityTest]
    public IEnumerator TestCardsMatchPngFrames()
    {
        if (!Application.isPlaying)
        {
            Assert.Ignore("PlayMode only.");
            yield break;
        }

        foreach (var step in VerifySet("HapTestPlayer", HapMoviePath, HapFramesDirName))
            yield return step;
        foreach (var step in VerifySet("HapQTestPlayer", HapQMoviePath, HapQFramesDirName))
            yield return step;
    }

    IEnumerable VerifySet(string objectName, string moviePath, string framesDirName)
    {
        var go = new GameObject(objectName);
        RenderTexture targetTexture = null;
        Texture2D readbackTexture = null;

        try
        {
            var player = go.AddComponent<HapPlayer>();
            player.Open(moviePath, HapPlayer.PathMode.StreamingAssets);
            player.loop = false;
            player.speed = 0;

            yield return null;
            player.UpdateNow();

            var pathInfo = $"Resolved path: {player.resolvedFilePath}";
            Assert.That(player.isValid, Is.True, pathInfo);
            Assert.That(player.frameCount, Is.GreaterThanOrEqualTo(FramePngs.Length), pathInfo);
            Assert.That(player.frameWidth, Is.GreaterThan(0), pathInfo);
            Assert.That(player.frameHeight, Is.GreaterThan(0), pathInfo);
            Assert.That(player.texture, Is.Not.Null, pathInfo);

            targetTexture = new RenderTexture(
                player.frameWidth, player.frameHeight, 0, RenderTextureFormat.ARGB32
            );
            targetTexture.wrapMode = TextureWrapMode.Clamp;
            player.targetTexture = targetTexture;
            readbackTexture = new Texture2D(
                player.frameWidth, player.frameHeight, TextureFormat.RGBA32, false
            );

            var duration = (float)player.streamDuration;
            var frameCount = player.frameCount;
            var dt = duration / frameCount;

            for (var i = 0; i < FramePngs.Length; i++)
            {
                player.time = i * dt + dt * 0.1f;

                player.UpdateNow();
                yield return null;
                yield return null;

                ReadRenderTexture(targetTexture, readbackTexture);
                var actualTexture = readbackTexture;

                var expected = LoadExpectedTexture(framesDirName, FramePngs[i]);
                try
                {
                    var ratio = ComputeMatchRatio(actualTexture, expected);
                    var message = $"{pathInfo} Frame:{i} Expected:{FramePngs[i]} Match:{ratio:0.000}";
                    Assert.That(ratio, Is.GreaterThanOrEqualTo(MatchThreshold), message);
                }
                finally
                {
                    Object.Destroy(expected);
                }

                yield return null;
                yield return null;
            }
        }
        finally
        {
            if (targetTexture != null)
                targetTexture.Release();
            Object.Destroy(targetTexture);
            Object.Destroy(readbackTexture);
            Object.Destroy(go);
        }
    }

    static Texture2D LoadExpectedTexture(string framesDirName, string fileName)
    {
        var path = Path.Combine(Application.streamingAssetsPath, framesDirName, fileName);
        var data = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(data);
        return tex;
    }

    static float ComputeMatchRatio(Texture2D actual, Texture2D expected)
    {
        var width = Mathf.Min(actual.width, expected.width);
        var height = Mathf.Min(actual.height, expected.height);

        var match = 0;
        var total = width * height;

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var a = actual.GetPixel(x, y);
                var e = expected.GetPixel(x, y);
                if (IsMatch(a, e)) match++;
            }

        return total > 0 ? (float)match / total : 0;
    }

    static bool IsMatch(Color actual, Color expected)
        => Mathf.Abs(actual.r - expected.r) <= ChannelTolerance
           && Mathf.Abs(actual.g - expected.g) <= ChannelTolerance
           && Mathf.Abs(actual.b - expected.b) <= ChannelTolerance;

    static void ReadRenderTexture(RenderTexture source, Texture2D destination)
    {
        var previous = RenderTexture.active;
        RenderTexture.active = source;
        destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        destination.Apply();
        RenderTexture.active = previous;
    }
}
