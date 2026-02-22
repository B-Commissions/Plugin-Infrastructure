using System.Collections.Generic;
using BlueBeard.Core;
using BlueBeard.Effects.Audiences;
using UnityEngine;

namespace BlueBeard.Effects;

public class EffectEmitterManager : IManager
{
    private readonly List<EffectEmitter> _emitters = new();
    public IReadOnlyList<EffectEmitter> Emitters => _emitters;

    public void Load() { }

    public void Unload()
    {
        foreach (var emitter in _emitters)
        {
            if (emitter != null) { emitter.End(); Object.Destroy(emitter.gameObject); }
        }
        _emitters.Clear();
    }

    public EffectEmitter Start(EffectDefinition definition, IEffectAudience audience)
    {
        var go = new GameObject($"EffectEmitter_{definition.EffectId}");
        var emitter = go.AddComponent<EffectEmitter>();
        emitter.Definition = definition;
        emitter.Audience = audience;
        emitter.Completed += _ => Stop(emitter);
        emitter.Begin();
        _emitters.Add(emitter);
        return emitter;
    }

    public void Stop(EffectEmitter emitter)
    {
        if (emitter == null) return;
        emitter.End();
        _emitters.Remove(emitter);
        Object.Destroy(emitter.gameObject);
    }
}
