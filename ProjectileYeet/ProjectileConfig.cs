using System.Collections.Generic;

namespace ProjectileYeet
{
	/// <summary>
	/// Configuration class for projectile sprite hiding settings.
	/// Similar to the working mod's config approach.
	/// </summary>
	public class ProjectileConfig
	{
		/// <summary>
		/// List of projectile names to hide visual sprites for.
		/// </summary>
		public List<string> HiddenProjectiles { get; set; } = new List<string>
		{
			"Sunder",
			"Fireball Explosion",
			"Poison Vial",
			"Dervish"
		};
	}
}
