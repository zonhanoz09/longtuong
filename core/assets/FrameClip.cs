using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>Shared immutable frame data. Playback clocks belong to each actor/action.</summary>
public sealed class FrameClip
{
    public IReadOnlyList<Texture2D> Frames { get; }
    public IReadOnlyList<Vector2> Pivots { get; }
    public IReadOnlyList<IReadOnlyList<Vector2>?> SourcePolygons { get; }
    public float DisplayScale { get; }
    public double Fps { get; }
    public bool Loop { get; }
    public int EventFrame { get; }
    public double Duration => Frames.Count / Fps;
    public double EventTime => EventFrame / Fps;
    public FrameClip(IEnumerable<Texture2D> frames, double fps, bool loop, int eventFrame, IEnumerable<Vector2>? pivots = null, float displayScale = 1, IEnumerable<IEnumerable<Vector2>?>? sourcePolygons = null)
    {
        var array = frames.ToArray();
        if (array.Length == 0 || !double.IsFinite(fps) || fps <= 0 || eventFrame < 0 || eventFrame >= array.Length)
            throw new ArgumentException("Invalid frame clip.");
        var points = pivots?.ToArray() ?? Enumerable.Repeat(new Vector2(0.5f, 460f/512f), array.Length).ToArray();
        if (points.Length != array.Length || points.Any(p => !p.IsFinite() || p.X < 0 || p.X > 1 || p.Y < 0 || p.Y > 1))
            throw new ArgumentException("Invalid frame pivots.");
        if (!float.IsFinite(displayScale) || displayScale < 0.25f || displayScale > 2) throw new ArgumentException("Invalid display scale.");
        var polygons = sourcePolygons?.Select(p => p == null ? null : (IReadOnlyList<Vector2>)Array.AsReadOnly(p.ToArray())).ToArray()
            ?? new IReadOnlyList<Vector2>?[array.Length];
        if (polygons.Length != array.Length) throw new ArgumentException("Invalid source polygons.");
        SourcePolygons = Array.AsReadOnly(polygons);
        DisplayScale = displayScale;
        Frames = Array.AsReadOnly(array);
        Pivots = Array.AsReadOnly(points);
        Fps = fps; Loop = loop; EventFrame = eventFrame;
    }
    public int FrameIndex(double time, bool? loop = null)
    {
        if (!double.IsFinite(time) || time < 0) time = 0;
        if (loop ?? Loop) time %= Duration;
        return (int)Math.Min(Math.Floor(time * Fps), Frames.Count - 1);
    }
    public Vector2 Pivot(double time) => Pivots[FrameIndex(time)];
    public Texture2D Sample(double time, bool? loop = null) => Frames[FrameIndex(time, loop)];
}
