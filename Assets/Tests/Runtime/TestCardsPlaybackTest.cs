using System.Collections;
using System.IO;
using Klak.Hap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TestCardsPlaybackTest
{
    const string MoviePath = "Tests/TestCards/TestCards.mov";
    const string FramesDirName = "Tests/TestCards";

    const float MatchThreshold = 0.99f;
    const float ChannelTolerance = 0.08f;
    const int SamplesPerAxis = 64;
    const bool FlipExpectedY = true;

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

        var go = new GameObject("HapTestPlayer");

        try
        {
            var player = go.AddComponent<HapPlayer>();
            player.Open(MoviePath, HapPlayer.PathMode.StreamingAssets);
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

            var duration = (float)player.streamDuration;
            var frameCount = player.frameCount;
            var dt = duration / frameCount;

            for (var i = 0; i < FramePngs.Length; i++)
            {
                player.time = i * dt + dt * 0.1f;

                player.UpdateNow();
                yield return null;
                yield return null;

                var expected = LoadExpectedTexture(FramePngs[i]);
                try
                {
                    var ratio = ComputeMatchRatio(player.texture, expected);
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
            Object.Destroy(go);
        }
    }

    static Texture2D LoadExpectedTexture(string fileName)
    {
        var path = Path.Combine(Application.streamingAssetsPath, FramesDirName, fileName);
        var data = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(data);
        return tex;
    }

    static float ComputeMatchRatio(Texture2D actual, Texture2D expected)
    {
        var width = Mathf.Min(actual.width, expected.width);
        var height = Mathf.Min(actual.height, expected.height);
        var stepX = Mathf.Max(1, width / SamplesPerAxis);
        var stepY = Mathf.Max(1, height / SamplesPerAxis);

        var match = 0;
        var total = 0;

        for (var y = stepY / 2; y < height; y += stepY)
            for (var x = stepX / 2; x < width; x += stepX)
            {
                var a = actual.GetPixel(x, y);
                var ey = FlipExpectedY ? height - 1 - y : y;
                var e = expected.GetPixel(x, ey);
                if (IsMatch(a, e)) match++;
                total++;
            }

        return total > 0 ? (float)match / total : 0;
    }

    static bool IsMatch(Color actual, Color expected)
        => Mathf.Abs(actual.r - expected.r) <= ChannelTolerance
           && Mathf.Abs(actual.g - expected.g) <= ChannelTolerance
           && Mathf.Abs(actual.b - expected.b) <= ChannelTolerance;
}
