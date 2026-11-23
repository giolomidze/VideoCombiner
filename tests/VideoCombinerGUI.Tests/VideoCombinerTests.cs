using System.Diagnostics;

namespace VideoCombinerGUI.Tests;

public class VideoCombinerTests
{
    [Fact]
    public void Constructor_NullPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new VideoCombiner(null!));
    }

    [Fact]
    public async Task CombineVideosAsync_NullFiles_Throws()
    {
        using var combiner = new VideoCombiner("ffmpeg.exe");

        await Assert.ThrowsAsync<ArgumentNullException>(() => combiner.CombineVideosAsync(null!, "out.mp4"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CombineVideosAsync_EmptyOutputName_Throws(string output)
    {
        using var combiner = new VideoCombiner("ffmpeg.exe");

        await Assert.ThrowsAsync<ArgumentException>(() => combiner.CombineVideosAsync(["a.mp4"], output));
    }

    [Fact]
    public async Task CombineVideosAsync_MissingFfmpeg_ThrowsFileNotFound()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"no-ffmpeg-{Guid.NewGuid():N}.exe");
        using var combiner = new VideoCombiner(missing);

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(
            () => combiner.CombineVideosAsync(["a.mp4", "b.mp4"], "out.mp4"));
        Assert.Equal(missing, ex.FileName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CombineVideosAsync_TwoClips_ProducesOutputAndReportsCompletion()
    {
        var ffmpeg = FindFfmpeg();
        if (ffmpeg is null)
            Assert.Skip("external/ffmpeg.exe not found.");

        var dir = Directory.CreateTempSubdirectory("videocombiner-tests-").FullName;
        try
        {
            var clipA = Path.Combine(dir, "a.mp4");
            var clipB = Path.Combine(dir, "b.mp4");
            await GenerateClipAsync(ffmpeg, clipA);
            await GenerateClipAsync(ffmpeg, clipB);

            var output = Path.Combine(dir, "combined.mp4");
            var progress = new List<double>();
            using var combiner = new VideoCombiner(ffmpeg);
            combiner.ProcessingProgressChanged += p => { lock (progress) progress.Add(p); };

            await combiner.CombineVideosAsync([clipA, clipB], output);

            var info = new FileInfo(output);
            Assert.True(info.Exists, "Combined output file was not created.");
            Assert.True(info.Length > 0, "Combined output file is empty.");
            Assert.NotEmpty(progress);
            Assert.Equal(100, progress[^1]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string? FindFfmpeg()
    {
        // Walk up from the test output folder to the repo root, which holds external/ffmpeg.exe.
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            var candidate = Path.Combine(d.FullName, "external", "ffmpeg.exe");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static async Task GenerateClipAsync(string ffmpeg, string path)
    {
        var psi = new ProcessStartInfo(ffmpeg,
            $"-y -loglevel error -f lavfi -i testsrc=duration=1:size=160x120:rate=10 -pix_fmt yuv420p \"{path}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };
        using var proc = Process.Start(psi)!;
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        Assert.True(proc.ExitCode == 0, $"ffmpeg failed to generate test clip: {stderr}");
    }
}
