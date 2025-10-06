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
		// Cache for processed transforms to avoid repeated expensive operations
		private static readonly HashSet<Transform> _processedTransforms = new HashSet<Transform>();

		// Pre-allocated arrays to avoid GC
		private static readonly Vector3 _hiddenOffset = new Vector3(0, -1000f, 0);

		/// <summary>
		/// Check if a GameObject has the ScaleToMatchProjectileRadius component
		/// This component is responsible for the visual scaling effects we want to hide
		/// </summary>
		private static bool HasScaleToMatchProjectileRadiusComponent(GameObject gameObject)
		{
			// Get all components on this GameObject and its children
			Component[] allComponents = gameObject.GetComponentsInChildren<Component>();

			foreach (Component component in allComponents)
			{
				// Check if this component's type name matches ScaleToMatchProjectileRadius
				if (component != null && component.GetType().Name == "ScaleToMatchProjectileRadius")
				{
					return true;
				}
			}

			return false;
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
		/// Intercepts Transform position changes and moves visual sprites of projectiles with ScaleToMatchProjectileRadius component off-screen.
		/// This keeps the projectile logic intact but hides the visual sprites.
		/// </summary>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Transform), "position", MethodType.Setter)]
		static void Postfix(Transform __instance)
		{
			// Skip if we've already processed this transform
			if (_processedTransforms.Contains(__instance))
				return;

			// Check if this GameObject has the ScaleToMatchProjectileRadius component
			if (!HasScaleToMatchProjectileRadiusComponent(__instance.gameObject))
				return;

			// Mark as processed to avoid repeated operations
			_processedTransforms.Add(__instance);

			// Hide the visual sprites
			HideProjectileVisuals(__instance);

			Debug.Log($"[ProjectileYeet] Hidden projectile with ScaleToMatchProjectileRadius: {__instance.name}");
		}

		/// <summary>
		/// Clear caches when mod is unloaded to prevent memory leaks
		/// </summary>
		public static void ClearCaches()
		{
			_processedTransforms.Clear();
		}
	}
}

