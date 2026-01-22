using UnityEngine;

public interface IDamageable
{
    void TakeDmg(float dmg);
}

///attach this to anything that can take damage so damage will be dealt regardless of what this is attached to
