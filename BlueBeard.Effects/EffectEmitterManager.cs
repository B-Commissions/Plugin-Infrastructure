using System.Collections.Generic;
using BlueBeard.Core;
using BlueBeard.Effects.Audiences;
using UnityEngine;

namespace BlueBeard.Effects;

public class EffectEmitterManager : IManager
{
    private readonly List<EffectEmitter> _emitters = [];
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

        // Add BEFORE Begin(): a OneShot definition has no yield, so its coroutine runs to
        // completion synchronously inside Begin() — including the Completed -> Stop(...)
        // handler. Adding afterwards used to leave a destroyed emitter in the list forever.
        _emitters.Add(emitter);
        emitter.Begin();
        return emitter;
    }

    public void Stop(EffectEmitter emitter)
    {
        if (emitter == null)
        {
            // Unity fake-null: a destroyed emitter still occupies a list slot — prune them.
            _emitters.RemoveAll(e => e == null);
            return;
        }
        emitter.End();
        _emitters.Remove(emitter);
        Object.Destroy(emitter.gameObject);
    }
}
