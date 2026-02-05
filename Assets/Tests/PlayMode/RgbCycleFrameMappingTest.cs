using System.Collections;
using Klak.Hap;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RgbCycleFrameMappingTest
{
    static readonly string[] TestPaths =
    {
        "Tests/RGBCycle/RGBCycle24.mov",
        "Tests/RGBCycle/RGBCycle24000-1001.mov",
        "Tests/RGBCycle/RGBCycle25.mov",
        "Tests/RGBCycle/RGBCycle30.mov",
        "Tests/RGBCycle/RGBCycle30000-1001.mov",
        "Tests/RGBCycle/RGBCycle50.mov",
        "Tests/RGBCycle/RGBCycle60.mov",
        "Tests/RGBCycle/RGBCycle60000-1001.mov"
    };

    enum RgbExpected { Red, Green, Blue }

    [UnityTest]
    public IEnumerator RgbCycleFrameMappingIsCorrect()
    {
        if (!Application.isPlaying)
        {
            Assert.Ignore("PlayMode only.");
            yield break;
        }

        foreach (var path in TestPaths)
            foreach (var step in VerifyFile(path))
                yield return step;
    }

    IEnumerable VerifyFile(string path)
    {
        var go = new GameObject("HapTestPlayer");

        try
        {
            var player = go.AddComponent<HapPlayer>();
            player.Open(path, HapPlayer.PathMode.StreamingAssets);
            player.loop = false;
            player.speed = 0;

            yield return null;
            player.UpdateNow();

            var pathInfo = $"Resolved path: {player.resolvedFilePath}";
            Assert.That(player.isValid, Is.True, pathInfo);
            Assert.That(player.frameCount, Is.GreaterThan(0), pathInfo);
            Assert.That(player.frameWidth, Is.GreaterThan(0), pathInfo);
            Assert.That(player.frameHeight, Is.GreaterThan(0), pathInfo);
            Assert.That(player.texture, Is.Not.Null, pathInfo);

            var duration = (float)player.streamDuration;
            var frameCount = player.frameCount;
            var dt = duration / frameCount;
            var maxFrames = Mathf.Min(frameCount, Mathf.FloorToInt(0.5f / dt) + 1);
            var cx = player.frameWidth / 2;
            var cy = player.frameHeight / 2;

            for (var i = 0; i < maxFrames; i++)
            {
                player.time = i * dt + dt * 0.1f;

                player.UpdateNow();
                yield return null;
                yield return null;

                var actual = player.texture.GetPixel(cx, cy);
                var expected = ExpectedForFrame(i);
                AssertRgbColor(actual, expected, pathInfo, i);
                yield return null;
                yield return null;
            }
        }
        finally
        {
            Object.Destroy(go);
        }
    }

    static RgbExpected ExpectedForFrame(int frameIndex)
        => (RgbExpected)(frameIndex % 3);

    static void AssertRgbColor(Color actual, RgbExpected expected, string pathInfo, int frameIndex)
    {
        var message = $"{pathInfo} Frame: {frameIndex} Color: {actual}";

        if (expected == RgbExpected.Red)
        {
            Assert.That(actual.r, Is.GreaterThan(0.8f), message);
            Assert.That(actual.g, Is.LessThan(0.2f), message);
            Assert.That(actual.b, Is.LessThan(0.2f), message);
        }
        else if (expected == RgbExpected.Green)
        {
            Assert.That(actual.g, Is.GreaterThan(0.8f), message);
            Assert.That(actual.r, Is.LessThan(0.2f), message);
            Assert.That(actual.b, Is.LessThan(0.2f), message);
        }
        else
        {
            Assert.That(actual.b, Is.GreaterThan(0.8f), message);
            Assert.That(actual.r, Is.LessThan(0.2f), message);
            Assert.That(actual.g, Is.LessThan(0.2f), message);
        }
    }
}
