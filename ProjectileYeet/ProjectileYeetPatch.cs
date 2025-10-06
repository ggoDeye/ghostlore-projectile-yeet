using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System;

namespace ProjectileYeet
{
	/// <summary>
	/// Harmony patch to hide visual sprites of specific projectiles.
	/// This keeps the projectile logic intact but hides the visual sprites.
	/// </summary>
	[HarmonyPatch]
	public class ProjectileYeetPatch
	{
		// Cache for projectile name checks to avoid repeated string operations
		private static readonly Dictionary<string, bool> _projectileNameCache = new Dictionary<string, bool>();
		private static readonly HashSet<string> _hiddenProjectileNames = new HashSet<string>();
		private static bool _cacheInitialized = false;

		// Cache for processed transforms to avoid repeated expensive operations
		private static readonly HashSet<Transform> _processedTransforms = new HashSet<Transform>();

		// Pre-allocated arrays to avoid GC
		private static readonly Vector3 _hiddenOffset = new Vector3(0, -1000f, 0);

		/// <summary>
		/// Initialize the cache with projectile names for faster lookups
		/// </summary>
		private static void InitializeCache()
		{
			if (_cacheInitialized || ModLoader.Config == null)
				return;

			_hiddenProjectileNames.Clear();
			foreach (string projectileName in ModLoader.Config.HiddenProjectiles)
			{
				_hiddenProjectileNames.Add(projectileName);
			}
			_cacheInitialized = true;
		}

		/// <summary>
		/// Fast check if a transform name contains any of our target projectile names
		/// </summary>
		private static bool IsTargetProjectile(string transformName)
		{
			if (!_cacheInitialized)
				InitializeCache();

			// Check cache first
			if (_projectileNameCache.TryGetValue(transformName, out bool cachedResult))
				return cachedResult;

			// Perform the check and cache the result
			bool isTarget = false;
			foreach (string projectileName in _hiddenProjectileNames)
			{
				if (transformName.Contains(projectileName))
				{
					isTarget = true;
					break;
				}
			}

			// Cache the result (limit cache size to prevent memory issues)
			if (_projectileNameCache.Count < 1000)
			{
				_projectileNameCache[transformName] = isTarget;
			}

			return isTarget;
		}

		/// <summary>
		/// Optimized method to hide visual sprites of a projectile
		/// </summary>
		private static void HideProjectileVisuals(Transform projectileTransform)
		{
			// Use direct child iteration instead of GetComponentsInChildren for better performance
			for (int i = 0; i < projectileTransform.childCount; i++)
			{
				Transform child = projectileTransform.GetChild(i);

				// Check if this child is a visual component
				string childName = child.name;
				if (childName.Contains("Sprite") || childName.Contains("Visual") || childName.Contains("Effect"))
				{
					// Move the visual sprite off-screen
					child.position += _hiddenOffset;
				}
			}
		}

		/// <summary>
		/// Intercepts Transform position changes and moves visual sprites of specific projectiles off-screen.
		/// This keeps the projectile logic intact but hides the visual sprites.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Transform), "position", MethodType.Setter)]
		static void Postfix(Transform __instance)
		{
			// Early exit if config not loaded
			if (ModLoader.Config == null)
				return;

			// Initialize cache if needed
			if (!_cacheInitialized)
				InitializeCache();

			// Skip if we've already processed this transform
			if (_processedTransforms.Contains(__instance))
				return;

			// Fast projectile name check
			if (!IsTargetProjectile(__instance.name))
				return;

			// Mark as processed to avoid repeated operations
			_processedTransforms.Add(__instance);

			// Hide the visual sprites
			HideProjectileVisuals(__instance);
		}

		/// <summary>
		/// Clear caches when mod is unloaded to prevent memory leaks
		/// </summary>
		public static void ClearCaches()
		{
			_projectileNameCache.Clear();
			_hiddenProjectileNames.Clear();
			_processedTransforms.Clear();
			_cacheInitialized = false;
		}
	}
}
