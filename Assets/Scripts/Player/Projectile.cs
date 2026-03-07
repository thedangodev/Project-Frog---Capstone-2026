using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseSpeed = 10f; //Adjust as nessisary 
    [SerializeField] private float baseDamage = 10f; //Adjust as nessisary 
    [SerializeField] private float maxScale = 2f; //Adjust as nessisary 

    [Header("Elemental VFX")]
    [SerializeField] private GameObject fireVFX;
    [SerializeField] private GameObject iceVFX;
    [SerializeField] private GameObject windVFX;

    private GameObject activeVFX;

    public float speed;
    public float damage;

    public void Initialize(float chargePercent, AnchorType anchor)
    {
        speed = Mathf.Lerp(baseSpeed, baseSpeed * 2f, chargePercent);
        damage = Mathf.Lerp(baseDamage, baseDamage * 3f, chargePercent);

        float scale = Mathf.Lerp(0.25f, maxScale, chargePercent); //Only exists to help visualize the charge's effect on the projectile in the absence of damage
        transform.localScale = Vector3.one * scale;

        // Spawn VFX based on the current anchor type
        SpawnVFX(anchor);

        Destroy(gameObject, 3f);
    }

    private void SpawnVFX(AnchorType anchor)
    {
        GameObject prefab = null;

        switch (anchor)
        {
            case AnchorType.Fire:
                prefab = fireVFX;
                break;
            case AnchorType.Ice:
                prefab = iceVFX;
                break;
            case AnchorType.Wind:
                prefab = windVFX;
                break;
        }

        if (prefab != null)
        {
            activeVFX = Instantiate(prefab, transform);
        }
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}