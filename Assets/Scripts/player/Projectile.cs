using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float baseSpeed = 10f; //Adjust as nessisary 
    [SerializeField] private float baseDamage = 10f; //Adjust as nessisary 
    [SerializeField] private float maxScale = 2f; //Adjust as nessisary 

    public float speed;
    public float damage;

    public void Initialize(float chargePercent)
    {
        speed = Mathf.Lerp(baseSpeed, baseSpeed * 2f, chargePercent);
        damage = Mathf.Lerp(baseDamage, baseDamage * 3f, chargePercent);

        float scale = Mathf.Lerp(0.25f, maxScale, chargePercent); //Only exists to help visualize the charge's effect on the projectile in the absence of damage
        transform.localScale = Vector3.one * scale;

        Destroy(gameObject, 3f);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}

