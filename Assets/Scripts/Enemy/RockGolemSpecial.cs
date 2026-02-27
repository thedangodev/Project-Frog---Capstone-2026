using System.Collections;
using UnityEngine;

public class RockGolemSpecial : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RockGolem2 owner;


    [Header("Special Settings")]
    [SerializeField] private float windupTime = 2f;   //how long the enemy winds up for
    [SerializeField] private float riseHeight = 3f;  //how far the fist rises
    [SerializeField] private float riseSpeed = 6f;

    [Header("Prefabs")]
    [SerializeField] private GameObject previewPrefab; 
    [SerializeField] private GameObject attackCylinderPrefab;

    private GameObject activePreview;

    public System.Action OnSpecialFinished;
    public void ActivateSpecial()
    {
        StartCoroutine(SpecialRoutine());
    }

    private IEnumerator SpecialRoutine()
    {
        // Stop movement
        owner.Agent.isStopped = true;

        //swtich golem's canSpecial to false
        owner.SetSpecialAvailable(false);

        // Determine strike position
        Vector3 strikePosition = owner.Player.position;
        strikePosition.y = 0f;

        // Spawn preview
        activePreview = Instantiate(previewPrefab, strikePosition, Quaternion.identity);

        // Wind-up delay
        yield return new WaitForSeconds(windupTime);

        // Remove preview
        if (activePreview != null)
            Destroy(activePreview);

        // Spawn attack cylinder below ground
        Vector3 spawnPosition = strikePosition - Vector3.up * riseHeight;
        GameObject cylinder = Instantiate(attackCylinderPrefab, spawnPosition, Quaternion.identity);

        // Rise effect
        float travelled = 0f;
        while (travelled < riseHeight)
        {
            float step = riseSpeed * Time.deltaTime;
            cylinder.transform.position += Vector3.up * step;
            travelled += step;
            yield return null;
        }

        Destroy(cylinder, 1f);

        // Re-enable movement
        owner.Agent.isStopped = false;

        // Call cooldown reset
        OnSpecialFinished?.Invoke();
    }
}
