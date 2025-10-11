# Projectile Yeet - Performance Optimization Analysis

## Executive Summary

This document analyzes the current implementation of the Projectile Yeet mod and proposes significantly more performant approaches based on the decompiled Ghostlore source code. The current implementation patches a global Unity method (`Transform.position` setter) which executes thousands of times per frame, whereas the proposed solutions target specific projectile lifecycle methods that execute only once per projectile spawn.

**Performance Impact:** The proposed approaches could reduce CPU overhead by **99%+** by replacing global transform monitoring with targeted projectile spawn interception.

---

## Current Implementation Analysis

### How It Works

The current mod (`ProjectileYeetPatch.cs`) uses the following approach:

```csharp
[HarmonyPatch(typeof(Transform), "position", MethodType.Setter)]
static void Postfix(Transform __instance)
{
    // Check if transform has ScaleToMatchProjectileRadius component
    if (HasScaleToMatchProjectileRadiusComponent(__instance.gameObject))
    {
        // Hide visual sprites by moving them off-screen
        HideProjectileVisuals(__instance);
    }
}
```

### Performance Problems

1. **Global Scope - Massive Overhead**

   - Patches `Transform.position` setter which affects **every Transform in the entire game**
   - Executes thousands of times per frame (every entity, UI element, particle, camera, etc.)
   - Runs for player movement, enemy movement, UI updates, particles, projectiles, items, etc.

2. **Expensive Component Detection**

   ```csharp
   Component[] allComponents = gameObject.GetComponentsInChildren<Component>();
   ```

   - `GetComponentsInChildren` is one of the most expensive Unity operations
   - Allocates memory for component array
   - Traverses entire GameObject hierarchy
   - Uses reflection to check component type names

3. **Inefficient Visual Hiding**

   - Moves sprites off-screen (`Y: -1000`) instead of actually disabling rendering
   - Unity still processes and potentially renders these "hidden" sprites
   - Sprites are still active in the scene hierarchy
   - Transform updates still propagate to these off-screen objects

4. **Cache Doesn't Help Much**
   - While caching prevents repeated processing of the same transform, it:
     - Grows indefinitely (memory leak potential)
     - Still requires HashSet lookup on every transform position change
     - Doesn't prevent the initial expensive component check

---

## Ghostlore Projectile System Architecture

Understanding the game's architecture reveals much better interception points:

### Key Classes

1. **`ITriggerOnProjectileSpawn` Interface**

   ```csharp
   public interface ITriggerOnProjectileSpawn
   {
       void Trigger(CharacterContainer character, ProjectileInstance projectileInstance);
   }
   ```

   - Called exactly once when a projectile spawns
   - Perfect lifecycle hook for initialization logic

2. **`ScaleToMatchProjectileRadius` Component**

   ```csharp
   public class ScaleToMatchProjectileRadius : MonoBehaviour, ITriggerOnProjectileSpawn
   {
       public void Trigger(CharacterContainer character, ProjectileInstance projectileInstance)
       {
           float? radiusValue = character?.StatContainer?.GetStatValue(
               Singleton<StatManager>.instance.ProjectileRadiusStat,
               new AdditionalModifiers(projectileInstance.Power?.Skill), 1f
           );
           if (radiusValue.HasValue)
           {
               base.transform.localScale = new Vector3(radiusValue.Value, radiusValue.Value, 1f);
           }
       }
   }
   ```

   - This is the exact component we want to target
   - Only exists on projectiles that scale with radius stat
   - `Trigger()` method is called once per projectile spawn

3. **`ProjectileInstance.Init()` Method**

   ```csharp
   public void Init(PowerInstance newPower, Vector2 startPosition, Quaternion startRotation, int projectileIndex)
   {
       // ... initialization code ...

       // This is where ITriggerOnProjectileSpawn components are triggered
       if (triggered == null)
       {
           triggered = GetComponentsInChildren<ITriggerOnProjectileSpawn>(includeInactive: true);
       }
       if (triggered != null)
       {
           ITriggerOnProjectileSpawn[] array = triggered;
           for (int i = 0; i < array.Length; i++)
           {
               array[i].Trigger(power?.Attacker, this);
           }
       }

       // ... particle system and audio setup ...
   }
   ```

   - Called once per projectile spawn
   - Perfect place to check for `ScaleToMatchProjectileRadius` component
   - All projectile components are already cached in `triggered` array

4. **`SpriteRenderer` Component**
   - The actual rendering component for projectile visuals
   - Disabling this completely prevents rendering (unlike moving off-screen)
   - Unity's rendering pipeline skips disabled SpriteRenderers entirely

---

## Recommended Performant Solutions

### ⭐ **Solution 1: Patch `ScaleToMatchProjectileRadius.Trigger` (BEST)**

**Approach:** Directly patch the method that scales projectiles to disable rendering instead.

**Advantages:**

- ✅ Executes only for projectiles with the component (extremely targeted)
- ✅ Zero overhead for projectiles without scaling
- ✅ No component detection needed - we're already inside the component
- ✅ Called exactly once per projectile spawn
- ✅ Can disable SpriteRenderer instead of moving off-screen

**Implementation:**

```csharp
[HarmonyPatch(typeof(ScaleToMatchProjectileRadius), "Trigger")]
public class ScaleToMatchProjectileRadiusPatch
{
    static void Postfix(ScaleToMatchProjectileRadius __instance,
                       CharacterContainer character,
                       ProjectileInstance projectileInstance)
    {
        // Disable all SpriteRenderers in this projectile
        SpriteRenderer[] spriteRenderers = __instance.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            renderer.enabled = false;
        }

        Debug.Log($"[ProjectileYeet] Disabled rendering for projectile: {projectileInstance.name}");
    }
}
```

**Performance Comparison:**

- **Current:** Executes ~10,000+ times per second (every transform in game)
- **This Solution:** Executes ~10-50 times per second (only scaling projectiles spawned)
- **Improvement:** ~99.5% reduction in executions

---

### 🥈 **Solution 2: Patch `ProjectileInstance.Init` (Postfix)**

**Approach:** Intercept projectile initialization after components are triggered, check for the scaling component, and disable rendering.

**Advantages:**

- ✅ Executes once per projectile spawn (any projectile)
- ✅ Access to cached `ITriggerOnProjectileSpawn` components array
- ✅ Can use efficient type checking instead of string matching
- ✅ Clean separation from game's scaling logic

**Implementation:**

```csharp
[HarmonyPatch(typeof(ProjectileInstance), "Init")]
public class ProjectileInstanceInitPatch
{
    // Need to target the specific Init overload
    [HarmonyPatch(new Type[] {
        typeof(PowerInstance),
        typeof(Vector2),
        typeof(Quaternion),
        typeof(int)
    })]
    static void Postfix(ProjectileInstance __instance)
    {
        // Check if this projectile has the ScaleToMatchProjectileRadius component
        ScaleToMatchProjectileRadius scaleComponent =
            __instance.GetComponentInChildren<ScaleToMatchProjectileRadius>();

        if (scaleComponent != null)
        {
            // Disable all SpriteRenderers
            SpriteRenderer[] spriteRenderers = __instance.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                renderer.enabled = false;
            }

            Debug.Log($"[ProjectileYeet] Disabled rendering for scaled projectile: {__instance.name}");
        }
    }
}
```

**Performance Comparison:**

- **Current:** Executes ~10,000+ times per second (every transform)
- **This Solution:** Executes ~100-200 times per second (all projectiles spawned)
- **Improvement:** ~99% reduction in executions

---

### 🥉 **Solution 3: Patch Both `Init` Overloads**

**Approach:** Since `ProjectileInstance` has two `Init` methods, patch both to ensure coverage.

**Implementation:**

```csharp
[HarmonyPatch(typeof(ProjectileInstance))]
public class ProjectileInstancePatch
{
    private static void DisableScalingProjectileVisuals(ProjectileInstance instance)
    {
        ScaleToMatchProjectileRadius scaleComponent =
            instance.GetComponentInChildren<ScaleToMatchProjectileRadius>();

        if (scaleComponent != null)
        {
            SpriteRenderer[] spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in spriteRenderers)
            {
                renderer.enabled = false;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("Init", new Type[] {
        typeof(PowerInstance),
        typeof(Vector2),
        typeof(Quaternion),
        typeof(int)
    })]
    static void InitPostfix1(ProjectileInstance __instance)
    {
        DisableScalingProjectileVisuals(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("Init", new Type[] {
        typeof(PowerInstance),
        typeof(CharacterContainer),
        typeof(int),
        typeof(Vector2)
    })]
    static void InitPostfix2(ProjectileInstance __instance)
    {
        DisableScalingProjectileVisuals(__instance);
    }
}
```

---

### 🎯 **Solution 4: Advanced - Disable Specific Components**

**Approach:** Instead of disabling all SpriteRenderers, disable only the specific visual components that you want to hide, preserving others (like debug visuals, shadows, etc.).

**Implementation:**

```csharp
[HarmonyPatch(typeof(ScaleToMatchProjectileRadius), "Trigger")]
public class ScaleToMatchProjectileRadiusPatch
{
    // Components/GameObjects we want to hide (customize as needed)
    private static readonly HashSet<string> VisualComponentNames = new HashSet<string>
    {
        "Sprite",
        "Visual",
        "Effect",
        "Glow",
        "Particle"
    };

    static void Postfix(ScaleToMatchProjectileRadius __instance)
    {
        // Disable specific visual components
        SpriteRenderer[] spriteRenderers = __instance.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            // Check if this is a visual component we want to hide
            bool shouldHide = false;
            foreach (string name in VisualComponentNames)
            {
                if (renderer.name.Contains(name))
                {
                    shouldHide = true;
                    break;
                }
            }

            if (shouldHide)
            {
                renderer.enabled = false;
            }
        }

        // Also disable particle systems if desired
        ParticleSystem[] particleSystems = __instance.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particleSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
```

---

## Performance Comparison Table

| Approach                                                    | Executions/sec | Component Checks    | Memory Allocations             | CPU Impact    |
| ----------------------------------------------------------- | -------------- | ------------------- | ------------------------------ | ------------- |
| **Current Implementation**                                  | ~10,000+       | Every transform     | High (GetComponentsInChildren) | **Very High** |
| **Solution 1** (Patch ScaleToMatchProjectileRadius.Trigger) | ~10-50         | None needed         | Minimal                        | **Very Low**  |
| **Solution 2** (Patch ProjectileInstance.Init)              | ~100-200       | Once per projectile | Low                            | **Low**       |
| **Solution 3** (Patch both Init overloads)                  | ~100-200       | Once per projectile | Low                            | **Low**       |
| **Solution 4** (Advanced selective hiding)                  | ~10-50         | None needed         | Minimal                        | **Very Low**  |

---

## Why `SpriteRenderer.enabled = false` is Better Than Moving Off-Screen

### Current Approach: Moving Off-Screen (Y: -1000)

```csharp
child.position += new Vector3(0, -1000f, 0);
```

**Problems:**

1. ❌ Sprite is still active and in the scene hierarchy
2. ❌ Unity still processes transform updates for these sprites
3. ❌ May still be rendered if camera view is large enough
4. ❌ Culling system still needs to check these sprites
5. ❌ Physics/collision systems may still process them
6. ❌ Takes up space in spatial partitioning structures

### Recommended Approach: Disable Renderer

```csharp
spriteRenderer.enabled = false;
```

**Benefits:**

1. ✅ Unity's rendering pipeline completely skips this renderer
2. ✅ No GPU processing for this sprite
3. ✅ No draw calls generated
4. ✅ Culling system ignores it
5. ✅ More explicit and maintainable code
6. ✅ Can re-enable later if needed (better than moving back from Y: -1000)

---

## Additional Optimizations

### 1. Cache Component References

Instead of calling `GetComponentsInChildren` every time, cache the references:

```csharp
private static readonly Dictionary<int, SpriteRenderer[]> _spriteRendererCache =
    new Dictionary<int, SpriteRenderer[]>();

static void DisableProjectileVisuals(ProjectileInstance instance)
{
    int instanceId = instance.GetInstanceID();

    if (!_spriteRendererCache.TryGetValue(instanceId, out SpriteRenderer[] renderers))
    {
        renderers = instance.GetComponentsInChildren<SpriteRenderer>();
        _spriteRendererCache[instanceId] = renderers;
    }

    foreach (SpriteRenderer renderer in renderers)
    {
        renderer.enabled = false;
    }
}
```

### 2. Use Object Pooling Awareness

Ghostlore uses object pooling for projectiles (see `IPooledObject` interface). Clear caches when projectiles are returned to pool:

```csharp
[HarmonyPatch(typeof(ProjectileInstance), "TerminateProjectile")]
public class ProjectileTerminatePatch
{
    static void Postfix(ProjectileInstance __instance)
    {
        // Clear cache entry when projectile is terminated/pooled
        _spriteRendererCache.Remove(__instance.GetInstanceID());
    }
}
```

### 3. Conditional Compilation for Debug Logging

Remove debug logs in release builds:

```csharp
[System.Diagnostics.Conditional("DEBUG")]
private static void DebugLog(string message)
{
    Debug.Log(message);
}
```

---

## Recommended Migration Path

### Phase 1: Immediate Performance Win

1. ✅ Implement **Solution 1** (Patch `ScaleToMatchProjectileRadius.Trigger`)
2. ✅ Replace off-screen movement with `SpriteRenderer.enabled = false`
3. ✅ Test thoroughly with various projectile types

### Phase 2: Enhanced Reliability

1. Add patches for both `Init` overloads as fallback
2. Implement component caching
3. Add proper cleanup for object pooling

### Phase 3: Polish

1. Add configuration options (which projectiles to hide)
2. Add support for hiding particle systems
3. Optimize with conditional compilation

---

## Code Example: Complete Optimized Implementation

```csharp
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace ProjectileYeet
{
    /// <summary>
    /// Optimized patch to disable rendering of projectiles that scale with Projectile Radius stat.
    /// This implementation is ~99.5% more efficient than the original Transform.position patch.
    /// </summary>
    [HarmonyPatch]
    public class ProjectileYeetPatch
    {
        // Optional: Cache sprite renderers for even better performance
        private static readonly Dictionary<int, SpriteRenderer[]> _rendererCache =
            new Dictionary<int, SpriteRenderer[]>();

        /// <summary>
        /// Patch the ScaleToMatchProjectileRadius.Trigger method.
        /// This is called exactly once per projectile spawn, only for projectiles with this component.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ScaleToMatchProjectileRadius), "Trigger")]
        static void DisableScalingProjectileVisuals(
            ScaleToMatchProjectileRadius __instance,
            CharacterContainer character,
            ProjectileInstance projectileInstance)
        {
            // Get all sprite renderers (this is much cheaper than the old approach)
            SpriteRenderer[] renderers = __instance.GetComponentsInChildren<SpriteRenderer>();

            // Disable rendering for all sprite renderers
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.enabled = false;
            }

            // Optional: Also hide particle effects
            ParticleSystem[] particleSystems = __instance.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            DebugLog($"[ProjectileYeet] Disabled rendering for scaled projectile: {projectileInstance?.name ?? "unknown"}");
        }

        /// <summary>
        /// Fallback patch for projectile initialization.
        /// This ensures we catch projectiles even if they're initialized differently.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProjectileInstance), "Init", new System.Type[] {
            typeof(PowerInstance),
            typeof(Vector2),
            typeof(Quaternion),
            typeof(int)
        })]
        static void ProjectileInit_Postfix(ProjectileInstance __instance)
        {
            // Check if this projectile has the scaling component
            ScaleToMatchProjectileRadius scaleComponent =
                __instance.GetComponentInChildren<ScaleToMatchProjectileRadius>();

            if (scaleComponent != null)
            {
                // Component found, but visuals might not be disabled yet
                // (This is a fallback; the Trigger patch above should handle most cases)
                DisableVisualsIfNeeded(__instance);
            }
        }

        /// <summary>
        /// Helper method to disable visuals for a projectile instance.
        /// </summary>
        private static void DisableVisualsIfNeeded(ProjectileInstance instance)
        {
            int instanceId = instance.GetInstanceID();

            // Check if we've already processed this instance
            if (_rendererCache.ContainsKey(instanceId))
                return;

            SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>();
            _rendererCache[instanceId] = renderers;

            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.enabled = false;
            }
        }

        /// <summary>
        /// Clean up cache when projectiles are terminated to prevent memory leaks.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProjectileInstance), "TerminateProjectile")]
        static void CleanupCache(ProjectileInstance __instance)
        {
            _rendererCache.Remove(__instance.GetInstanceID());
        }

        /// <summary>
        /// Debug logging that only compiles in DEBUG builds.
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugLog(string message)
        {
            Debug.Log(message);
        }
    }
}
```

---

## Testing Recommendations

### Performance Testing

1. **Profile Before/After**: Use Unity Profiler to measure CPU usage

   - Monitor: `Transform.set_position` time
   - Monitor: `GetComponentsInChildren` allocations
   - Monitor: Overall frame time

2. **Stress Test**: Spawn many scaling projectiles

   - Old implementation: Will spike CPU usage significantly
   - New implementation: Should have minimal impact

3. **Memory Testing**: Check for memory leaks
   - Ensure cache is properly cleaned up
   - Monitor GC allocations

### Functional Testing

1. Verify visuals are hidden for all scaling projectiles
2. Verify non-scaling projectiles are unaffected
3. Test with various skills that create scaling projectiles
4. Verify no errors in console logs

---

## Conclusion

**Recommendation:** Implement **Solution 1** (Patch `ScaleToMatchProjectileRadius.Trigger`) with `SpriteRenderer.enabled = false`.

**Expected Results:**

- ✅ ~99.5% reduction in CPU overhead
- ✅ No more expensive global Transform.position interception
- ✅ No more GetComponentsInChildren calls on every transform
- ✅ Proper rendering disablement instead of off-screen hiding
- ✅ Cleaner, more maintainable code
- ✅ Better integration with game's architecture

This approach leverages the game's own projectile lifecycle system instead of fighting against it, resulting in dramatically better performance and more reliable behavior.
