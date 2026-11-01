using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LongTuong;

/// <summary>Samples one action on a single clock. Never mutates the combat engine.</summary>
public sealed class BattlePresentationController : IDisposable
{
    private readonly Control _host;
    private readonly IReadOnlyDictionary<Fighter, BattleHeroCell> _cells;
    private readonly BattleAction _action;
    private readonly Action<FighterSnapshot> _commit;
    private readonly BattleActor _actor;
    private readonly string _pose;
    private readonly bool _ranged;
    private readonly Vector2 _home, _start, _destination;
    private readonly int _originalZ;
    private readonly double _approach, _returnStart, _returnDuration;
    private readonly FrameClip? _impactClip, _castClip, _projectileClip;
    private readonly List<Node> _owned = new();
    private readonly List<(BattleProjectile Node, Vector2 From, Vector2 To)> _bolts = new();
    private readonly List<(Node2D Node, double Start, double Duration)> _effects = new();
    private readonly List<(Label Node, Vector2 Start)> _labels = new();
    private readonly Dictionary<Fighter, FighterSnapshot> _after;
    private bool _released, _impacted, _disposed;
    public double Time { get; private set; }
    public double ReleaseAt { get; }
    public double ImpactAt { get; }
    public double Duration { get; }
    public bool Completed { get; private set; }
    public int ReleaseCount { get; private set; }
    public int ImpactCount { get; private set; }

    public BattlePresentationController(Control host, IReadOnlyDictionary<Fighter, BattleHeroCell> cells,
        BattleAction action, Action<FighterSnapshot> commit)
    {
        _host = host; _cells = cells; _action = action; _commit = commit;
        _actor = cells[action.Actor].Actor!;
        _pose = action.Ultimate ? "ultimate" : "attack";
        _ranged = _actor.Art.Ranged || (action.Ultimate && action.Actor.Def.UltKind != UltKind.SingleDamage);
        _home = _actor.Position; _start = _actor.GlobalPosition; _originalZ = _actor.ZIndex;
        _after = action.After.ToDictionary(s => s.Fighter);
        _destination = !_ranged && action.Impacts.Count > 0
            ? cells[action.Impacts[0].Target].Actor!.GlobalPosition - new Vector2(action.Actor.IsPlayer ? 65 : -65, 0)
            : _start;
        _approach = _ranged ? 0 : 0.18;
        _returnDuration = _ranged ? 0 : 0.18;
        var clip = _actor.PoseClip(_pose);
        ReleaseAt = _approach + (clip?.EventTime ?? 0.11);
        bool travel = _ranged && action.Impacts.Any(i => i.Target != action.Actor);
        ImpactAt = ReleaseAt + (travel ? 0.18 : 0);
        string id = action.Actor.Def.Id;
        bool heal = action.Impacts.Any(i => i.Healing);
        _impactClip = AssetCatalog.Effect(id, action.Ultimate ? "ultimate" : "normal")
            ?? (heal ? AssetCatalog.SharedEffect("heal") : !action.Ultimate ? AssetCatalog.SharedEffect("hit") : null);
        _castClip = AssetCatalog.Effect(id, "cast");
        _projectileClip = AssetCatalog.Effect(id, "projectile") ?? AssetCatalog.SharedEffect("projectile");
        double reaction = action.Impacts.Where(i => !i.Dodged && !i.Healing).Select(i =>
            cells[i.Target].Actor!.PoseClip(_after[i.Target].Hp <= 0 ? "death" : "hit")?.Duration ?? 0.16).DefaultIfEmpty(0).Max();
        _returnStart = Math.Max(_approach + (clip?.Duration ?? 0.22),
            Math.Max(ImpactAt + Math.Max(reaction, Math.Max(0.24, _impactClip?.Duration ?? 0.24)), ReleaseAt + (_castClip?.Duration ?? 0)));
        Duration = _returnStart + _returnDuration + 0.08;
        _actor.ZIndex = 3;
        SampleActor(0);
    }

    public void Advance(double delta)
    {
        if (_disposed || Completed || !double.IsFinite(delta) || delta < 0) return;
        Time += delta;
        // Threshold crossing, not frame equality: low FPS/x3 cannot skip or repeat an event.
        if (!_released && Time >= ReleaseAt) { SampleActor(ReleaseAt); Release(); }
        if (!_impacted && Time >= ImpactAt) Impact();
        SampleActor(Time);
        foreach (var (bolt, from, to) in _bolts)
        {
            bolt.Position = from.Lerp(to, Fraction(Time - ReleaseAt, ImpactAt - ReleaseAt));
            bolt.Time = Math.Max(0, Time - ReleaseAt);
            bolt.QueueRedraw();
        }
        if (_impacted)
        {
            foreach (var impact in _action.Impacts.Where(i => !i.Dodged && !i.Healing))
            {
                var target = _cells[impact.Target].Actor!;
                string pose = _after[impact.Target].Hp <= 0 ? "death" : "hit";
                double elapsed = Time - ImpactAt;
                double length = target.PoseClip(pose)?.Duration ?? 0.16;
                if (pose == "death" || elapsed < length)
                {
                    target.SamplePose(pose, elapsed);
                    target.Recoil = pose == "hit" && target.PoseClip("hit") == null
                        ? new Vector2((impact.Target.IsPlayer ? -1 : 1) * 6 * (1 - Fraction(elapsed, length)), 0) : Vector2.Zero;
                }
                else target.Idle();
            }
            foreach (var (label, start) in _labels)
            {
                float progress = Fraction(Time - ImpactAt, 0.24);
                label.Position = start - new Vector2(0, 30 * progress);
                label.Modulate = Colors.White.Lerp(Colors.Transparent, progress);
            }
        }
        foreach (var (node, start, duration) in _effects)
        {
            double age = Math.Max(0, Time - start);
            node.Visible = age < duration;
            if (node is BattleFrameEffect png) png.Time = age;
            if (node is BattleImpactVisual code) code.Progress = Fraction(age, duration);
            node.QueueRedraw();
        }
        if (Time >= Duration)
        {
            Completed = true;
            Dispose();
        }
    }

    private void SampleActor(double time)
    {
        if (time < _approach)
        {
            _actor.Idle();
            _actor.GlobalPosition = _start.Lerp(_destination, Ease(Fraction(time, _approach)));
        }
        else
        {
            _actor.GlobalPosition = _destination.Lerp(_start, Ease(Fraction(time - _returnStart, _returnDuration)));
            if (time >= _returnStart) _actor.Idle();
            else _actor.SamplePose(_pose, time - _approach);
        }
        _actor.Motion = 0;
        _actor.QueueRedraw();
    }

    private void Release()
    {
        _released = true; ReleaseCount++;
        if (_action.Ultimate)
        {
            var before = _action.Before.Single(s => s.Fighter == _action.Actor);
            _commit(before with { Rage = _after[_action.Actor].Rage });
        }
        if (_castClip != null) AddFrameEffect(_castClip, _actor.AnchorWorld("cast"), ReleaseAt);
        if (!_ranged || ImpactAt <= ReleaseAt) return;
        foreach (var impact in _action.Impacts.Where(i => i.Target != _action.Actor))
        {
            var from = _host.GetGlobalTransform().AffineInverse() * _actor.AnchorWorld("cast");
            var to = _host.GetGlobalTransform().AffineInverse() * _cells[impact.Target].Actor!.AnchorWorld("hit");
            var bolt = new BattleProjectile { Position = from, Direction = to - from, Tint = _actor.Accent,
                Weapon = _actor.Art.Weapon, Clip = _projectileClip, ZIndex = 3 };
            _host.AddChild(bolt); _owned.Add(bolt); _bolts.Add((bolt, from, to));
        }
    }

    private void Impact()
    {
        _impacted = true; ImpactCount++;
        foreach (var (bolt, _, _) in _bolts) { _owned.Remove(bolt); bolt.QueueFree(); }
        _bolts.Clear();
        _commit(_after[_action.Actor]);
        foreach (var impact in _action.Impacts)
        {
            _commit(_after[impact.Target]);
            var target = _cells[impact.Target].Actor!;
            var point = target.AnchorWorld("hit");
            if (!impact.Dodged)
            {
                if (_impactClip != null) AddFrameEffect(_impactClip, point, ImpactAt);
                else
                {
                    var effect = new BattleImpactVisual { Position = _host.GetGlobalTransform().AffineInverse() * point,
                        Tint = impact.Healing ? _actor.Accent.Lightened(0.2f) : _actor.Accent, Healing = impact.Healing,
                        Ultimate = _action.Ultimate, HeroId = _action.Actor.Def.Id, Motif = _actor.Art.Motif, ZIndex = 4 };
                    _host.AddChild(effect); _owned.Add(effect); _effects.Add((effect, ImpactAt, 0.24));
                }
            }
            string text = impact.Dodged ? "NÉ" : impact.Healing ? $"+{impact.Amount}" : $"−{impact.Amount}";
            if (impact.Critical) text += " BẠO!";
            if (impact.Blocked) text += " ĐỠ";
            var label = new Label { Text = text, Position = _host.GetGlobalTransform().AffineInverse() * point - new Vector2(45, 50), ZIndex = 5 };
            UiTheme.StyleBody(label, impact.Critical ? UiTheme.FontTitle : UiTheme.FontHeading);
            label.AddThemeColorOverride("font_color", impact.Healing ? UiTheme.HpColor : UiTheme.GoldHover);
            label.AddThemeColorOverride("font_outline_color", UiTheme.Background);
            label.AddThemeConstantOverride("outline_size", 5);
            _host.AddChild(label); _owned.Add(label); _labels.Add((label, label.Position));
        }
    }

    private void AddFrameEffect(FrameClip clip, Vector2 world, double start)
    {
        var effect = new BattleFrameEffect { Clip = clip, Position = _host.GetGlobalTransform().AffineInverse() * world, ZIndex = 4,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear };
        _host.AddChild(effect); _owned.Add(effect); _effects.Add((effect, start, clip.Duration));
    }
    private static float Fraction(double time, double duration) => duration <= 0 ? (time >= 0 ? 1 : 0) : (float)Math.Clamp(time / duration, 0, 1);
    private static float Ease(float value) => value * value * (3 - 2 * value);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (GodotObject.IsInstanceValid(_actor))
        {
            _actor.Position = _home; _actor.ZIndex = _originalZ; _actor.Motion = 0; _actor.Idle();
        }
        foreach (var impact in _action.Impacts)
            if (_cells.TryGetValue(impact.Target, out var cell) && GodotObject.IsInstanceValid(cell.Actor)) cell.Actor!.Idle();
        foreach (var node in _owned) if (GodotObject.IsInstanceValid(node)) node.QueueFree();
        _owned.Clear(); _bolts.Clear(); _effects.Clear(); _labels.Clear();
    }
}
