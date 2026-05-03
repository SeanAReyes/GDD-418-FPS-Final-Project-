namespace FPS.Weapons
{
    /// <summary>
    /// Implemented by any GameObject that can receive damage (enemies, destructibles, etc).
    /// Returns true if the hit was fatal.
    /// </summary>
    public interface IDamageable
    {
        bool TakeDamage(float damage);
    }
}
