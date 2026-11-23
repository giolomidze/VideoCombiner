using VideoMerger;

namespace VideoCombinerGUI.Tests;

public class FFmpegProgressParserTests
{
    private static List<double> Capture(FFmpegProgressParser parser)
    {
        var values = new List<double>();
        parser.ProgressChanged += values.Add;
        return values;
    }

    [Fact]
    public void Parse_TypicalStderrLine_ReportsPercentage()
    {
        var parser = new FFmpegProgressParser(totalDuration: 18);
        var values = Capture(parser);

        parser.Parse("frame=  220 fps=0.0 q=-1.0 Lsize=   13763kB time=00:00:09.00 bitrate=12522.5kbits/s speed=15.9x");

        Assert.Equal(50, Assert.Single(values), precision: 6);
    }

    [Fact]
    public void Parse_ProgressPipeOutTimeLine_ReportsPercentage()
    {
        var parser = new FFmpegProgressParser(totalDuration: 18);
        var values = Capture(parser);

        parser.Parse("out_time=00:00:09.000000");

        Assert.Equal(50, Assert.Single(values), precision: 6);
    }

    [Theory]
    [InlineData("time=00:00:00.00", 10, 0)]
    [InlineData("time=00:00:02.50", 10, 25)]
    [InlineData("time=00:01:00.00", 120, 50)]
    [InlineData("time=01:00:00.00", 7200, 50)]
    [InlineData("time=00:00:10.00", 10, 100)]
    public void Parse_VariousTimes_ReportsExpectedPercentage(string line, double total, double expected)
    {
        var parser = new FFmpegProgressParser(total);
        var values = Capture(parser);

        parser.Parse(line);

        Assert.Equal(expected, Assert.Single(values), precision: 6);
    }

    [Fact]
    public void Parse_TimeBeyondTotal_IsCappedAt100()
    {
        var parser = new FFmpegProgressParser(totalDuration: 5);
        var values = Capture(parser);

        parser.Parse("time=00:00:30.00");

        Assert.Equal(100, Assert.Single(values));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhitespace_RaisesNoEvent(string? line)
    {
        var parser = new FFmpegProgressParser(totalDuration: 10);
        var values = Capture(parser);

        parser.Parse(line!);

        Assert.Empty(values);
    }

    [Theory]
    [InlineData("progress=continue")]
    [InlineData("Input #0, mov,mp4,m4a,3gp,3g2,mj2, from 'a.mp4':")]
    [InlineData("time=N/A")]
    public void Parse_LineWithoutValidTime_RaisesNoEvent(string line)
    {
        var parser = new FFmpegProgressParser(totalDuration: 10);
        var values = Capture(parser);

        parser.Parse(line);

        Assert.Empty(values);
    }

    [Fact]
    public void Parse_ZeroTotalDuration_IsCappedAt100()
    {
        // Documents current behavior: x / 0 => +Infinity, which Math.Min caps to 100.
        var parser = new FFmpegProgressParser(totalDuration: 0);
        var values = Capture(parser);

        parser.Parse("time=00:00:01.00");

        Assert.Equal(100, Assert.Single(values));
    }
}
