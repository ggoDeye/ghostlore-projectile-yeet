using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace ProjectileYeet
{
	/// <summary>
	/// Entry point for the projectile yeet mod.
	/// This mod hides visual effects of specific projectiles while keeping their damage functionality.
	/// </summary>
	public class ModLoader : IModLoader
	{
		private static Harmony harmony;

		/// <summary>
		/// Called when mod is first loaded.
		/// </summary>
		public void OnCreated()
		{
			Debug.Log("[ProjectileYeet] OnCreated() called - Applying component-based patches...");

			try
			{
				// Initialize Harmony for patching with error handling
				harmony = new Harmony("com.projectile.yeet");
				Debug.Log("[ProjectileYeet] Harmony created, applying component-based patches...");
				harmony.PatchAll(Assembly.GetExecutingAssembly());
				Debug.Log("[ProjectileYeet] Component-based patches applied successfully");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[ProjectileYeet] Error applying patches: {ex.Message}");
			}
		}


		/// <summary>
		/// Called when mod is unloaded.
		/// </summary>
		public void OnReleased()
		{
			// Clean up patches
			if (harmony != null)
			{
				harmony.UnpatchAll("com.projectile.yeet");
				harmony = null;
			}

			// Clear caches to prevent memory leaks
			ProjectileYeetPatch.ClearCaches();
		}

		/// <summary>
		/// Called when game is loaded.
		/// </summary>
		/// <param name="mode">Either a new game, or a previously saved game.</param>
		public void OnGameLoaded(LoadMode mode)
		{
			Debug.Log("[ProjectileYeet] OnGameLoaded() called");
		}

		/// <summary>
		/// Called when game is unloaded.
		/// </summary>
		public void OnGameUnloaded()
		{
			Debug.Log("[ProjectileYeet] OnGameUnloaded() called");
		}
	}
}
