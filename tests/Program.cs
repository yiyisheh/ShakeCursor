using ShakeCursor;

// Each case feeds a synthetic pointer trace (logical px over ms) and checks whether a shake is detected.
var random = new Random(1);
var failures = 0;

Check("hard shake ±150px, 90ms per swing", true, t => (150 * Triangle(t, 90), 0), 1200);
Check("moderate shake ±100px, 120ms per swing", true, t => (100 * Triangle(t, 120), 0), 1200);
Check("sine shake ±120px, 110ms per swing", true, t => (120 * Math.Sin(t / 110 * Math.PI), 0), 1200);
Check("diagonal shake", true, t => (100 * Triangle(t, 100), 70 * Triangle(t, 100)), 1200);
Check("vertical shake", true, t => (0, 100 * Triangle(t, 100)), 1200);
Check("shake with ±4px hand jitter", true, t => (120 * Triangle(t, 100), 0), 1200, jitter: 4);
Check("1000 Hz mouse", true, t => (120 * Triangle(t, 100), 0), 1200, sampleMs: 1, jitter: 1.5);
Check("slow back and forth, 400ms per swing", false, t => (150 * Triangle(t, 400), 0), 3000);
Check("fast straight flick with jitter", false, t => (Math.Min(t, 300) * 5, 0), 600, jitter: 4);
Check("three fast flicks in the same direction",
    false, t => ((t % 250) < 150 ? (t % 250) * 6 + Math.Floor(t / 250) * 900 : 900 * Math.Floor(t / 250) + 900, 0), 750);
Check("fast circles, r=100, 300ms per turn",
    false, t => (100 * Math.Cos(t / 300 * 2 * Math.PI), 100 * Math.Sin(t / 300 * 2 * Math.PI)), 1500);
Check("one quick trip between two buttons", false, t => (t < 250 ? 200 * Triangle(t, 100) : 200 * Triangle(250, 100), 0), 800);
Check("small tremor ±15px", false, t => (15 * Triangle(t, 40), 0), 1500);

Console.WriteLine(failures == 0 ? "All passed" : $"{failures} failed");
return failures;

void Check(string name, bool expectShake, Func<double, (double X, double Y)> path, double durationMs,
           double sampleMs = 8, double jitter = 0)
{
    var detector = new ShakeDetector(new Config());
    double triggeredAt = -1, maxLevel = 0;
    for (double t = 0; t <= durationMs; t += sampleMs)
    {
        var (x, y) = path(t);
        x += (random.NextDouble() - 0.5) * 2 * jitter;
        y += (random.NextDouble() - 0.5) * 2 * jitter;
        if (detector.OnMove(x, y, t) && triggeredAt < 0)
            triggeredAt = t;
        maxLevel = Math.Max(maxLevel, detector.Tick(t, sampleMs));
    }

    var ok = (triggeredAt >= 0) == expectShake;
    if (!ok)
        failures++;
    var result = triggeredAt >= 0 ? $"triggered at {triggeredAt:0} ms" : "not triggered";
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name,-42} {result,-22} max level {maxLevel:0.00}");
}

static double Triangle(double t, double halfPeriod)
{
    var phase = t / halfPeriod % 2;
    return phase < 1 ? 2 * phase - 1 : 3 - 2 * phase;
}
