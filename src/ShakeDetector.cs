namespace ShakeCursor;

/// <summary>
/// Turns a stream of pointer moves (logical pixels, millisecond timestamps) into an enlargement Level from 0 to 1.
///
/// The path is cut into strokes: turning back (more than ~107°) ends the current stroke.
/// A stroke that is long, straight and fast enough, and opposite to the previous one, is a swing. MinReversals swings within WindowMs mean shaking;
/// each further swing raises Level. ReleaseDelayMs after the last swing, Level falls linearly to 0 over ShrinkMs.
/// Straightness (net distance / path >= 0.7) rules out fast circles. Nothing counts while a mouse button is down (dragging, selecting, games).
///
/// Threading: OnMove runs on the hook thread and Tick on the UI thread, so state is locked.
/// </summary>
sealed class ShakeDetector
{
    const double TurnCos = -0.3;          // turning more than ~107° ends a stroke
    const double MinStraightness = 0.7;
    const double MaxGapMs = 120;          // a longer pause means the hand stopped; start over

    readonly object gate = new();
    readonly Queue<double> swingTimes = new();   // end times of recent swings

    Config config;
    bool hasLast;
    double lastX, lastY, lastT;
    double strokeDx, strokeDy, strokeLen, strokeStartT;
    double lastSwingDx, lastSwingDy;
    double level, lastSwingT;

    public ShakeDetector(Config config) => this.config = config;

    public Config Config
    {
        set { lock (gate) config = value; }
    }

    /// <summary>Feeds one pointer move. Returns true when a shake has just been detected and the UI should start growing the cursor.</summary>
    public bool OnMove(double x, double y, double t)
    {
        lock (gate)
        {
            if (!hasLast || t - lastT > MaxGapMs)
            {
                SetLast(x, y, t);
                ResetStroke(t);
                return false;
            }

            double dx = x - lastX, dy = y - lastY;
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 0.5)
                return false;

            var prevT = lastT;
            SetLast(x, y, t);

            if (strokeLen > 0)
            {
                var net = Math.Sqrt(strokeDx * strokeDx + strokeDy * strokeDy);
                var cos = net > 0 ? (dx * strokeDx + dy * strokeDy) / (d * net) : 1;
                if (cos < TurnCos)
                {
                    var swung = EndStroke(prevT, net);
                    StartStroke(dx, dy, d, prevT);
                    return swung;
                }
            }
            else
            {
                strokeStartT = prevT;
            }

            strokeDx += dx;
            strokeDy += dy;
            strokeLen += d;
            return false;
        }
    }

    /// <summary>A mouse button went down: drop the current path.</summary>
    public void OnButton()
    {
        lock (gate)
        {
            swingTimes.Clear();
            ResetStroke(lastT);
        }
    }

    /// <summary>Advances time and returns the current Level.</summary>
    public double Tick(double t, double dtMs)
    {
        lock (gate)
        {
            if (level > 0 && t - lastSwingT > config.ReleaseDelayMs)
                level = Math.Max(0, level - dtMs / config.ShrinkMs);
            return level;
        }
    }

    bool EndStroke(double endT, double net)
    {
        // Too short: jitter at a turnaround. Neither a swing nor a break in the streak
        if (strokeLen < config.MinStroke)
            return false;

        var duration = Math.Max(1, endT - strokeStartT);
        var speed = strokeLen / duration * 1000;
        if (net / strokeLen < MinStraightness || speed < config.MinSpeed || Native.IsAnyMouseButtonDown())
        {
            swingTimes.Clear();
            return false;
        }

        // Swings must alternate; two in the same direction (a straight flick with a wobble) restart the streak
        if (strokeDx * lastSwingDx + strokeDy * lastSwingDy >= 0)
            swingTimes.Clear();
        lastSwingDx = strokeDx;
        lastSwingDy = strokeDy;

        swingTimes.Enqueue(endT);
        while (swingTimes.Count > 0 && endT - swingTimes.Peek() > config.WindowMs)
            swingTimes.Dequeue();

        // While already enlarged every swing counts; before that, enough swings are needed first
        if (level <= 0 && swingTimes.Count < config.MinReversals)
            return false;

        var wasIdle = level <= 0;
        level = Math.Min(1, level + config.GrowPerSwing * Math.Min(2, speed / config.MinSpeed));
        lastSwingT = endT;
        return wasIdle;
    }

    void StartStroke(double dx, double dy, double d, double t)
    {
        strokeDx = dx;
        strokeDy = dy;
        strokeLen = d;
        strokeStartT = t;
    }

    void ResetStroke(double t) => StartStroke(0, 0, 0, t);

    void SetLast(double x, double y, double t)
    {
        lastX = x;
        lastY = y;
        lastT = t;
        hasLast = true;
    }
}
