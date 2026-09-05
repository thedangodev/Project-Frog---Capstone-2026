// Shared enum describing how an enemy should react when struck by a player projectile.
// Regular attacks apply Stagger; charged attacks apply Knockback.
public enum HitReaction
{
    None,
    Stagger,
    Knockback,
    Block,
    TakeHit
}