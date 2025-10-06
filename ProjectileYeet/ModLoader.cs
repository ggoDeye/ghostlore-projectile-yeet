using UnityEngine;
using HarmonyLib;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;

namespace ProjectileYeet
{
	/// <summary>
	/// Entry point for the projectile yeet mod.
	/// This mod hides visual effects of specific projectiles while keeping their damage functionality.
	/// </summary>
	public class ModLoader : IModLoader
	{
		private static Harmony harmony;
		public static ProjectileConfig Config { get; private set; }

		/// <summary>
		/// Called when mod is first loaded.
		/// </summary>
		public void OnCreated()
		{
			Debug.Log("[ProjectileYeet] OnCreated() called - Loading config and applying patches...");

			try
			{
				// Load configuration
				LoadConfig();

				// Initialize Harmony for patching with error handling
				harmony = new Harmony("com.projectile.yeet");
				Debug.Log("[ProjectileYeet] Harmony created, applying safe patches...");
				harmony.PatchAll(Assembly.GetExecutingAssembly());
				Debug.Log("[ProjectileYeet] Safe patches applied successfully");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[ProjectileYeet] Error applying patches: {ex.Message}");
			}
		}

		/// <summary>
		/// Loads the projectile configuration from JSON file.
		/// </summary>
		private void LoadConfig()
		{
			try
			{
				string configPath = Path.Combine(Application.dataPath, "..", "Mod Folder", "projectile_config.json");
				if (File.Exists(configPath))
				{
					string json = File.ReadAllText(configPath);
					Config = JsonConvert.DeserializeObject<ProjectileConfig>(json);
					Debug.Log($"[ProjectileYeet] Config loaded from {configPath}");
				}
				else
				{
					// Create default config if file doesn't exist
					Config = new ProjectileConfig();
					SaveConfig();
					Debug.Log("[ProjectileYeet] Created default config");
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[ProjectileYeet] Error loading config: {ex.Message}");
				Config = new ProjectileConfig(); // Use default config on error
			}
		}

		/// <summary>
		/// Saves the projectile configuration to JSON file.
		/// </summary>
		private void SaveConfig()
		{
			try
			{
				string configPath = Path.Combine(Application.dataPath, "..", "Mod Folder", "projectile_config.json");
				string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
				File.WriteAllText(configPath, json);
				Debug.Log($"[ProjectileYeet] Config saved to {configPath}");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[ProjectileYeet] Error saving config: {ex.Message}");
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
