using UnityEngine;

public class FrogTongue : MonoBehaviour
{
    [SerializeField] private Transform tongueMesh;
    [SerializeField] private float maxLength = 10f;
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private float tongueWidth = 0.3f;

    private float currentLength = 0f;
    public bool extending = false; //keep public 
    public bool retracting = false; //keep public

    public void BeginTongue()
    {
        if (retracting) return;
        extending = true;
        retracting = false;
    }

    public void EndTongue()
    {
        if (!retracting)
        {
            extending = false;
            retracting = true;
        }
    }

    private void Update()
    {
        if (extending)
        {
            currentLength += extendSpeed * Time.deltaTime;
            if (currentLength >= maxLength)
            {
                currentLength = maxLength;
                extending = false;
                EndTongue();
            }
        } 
        else if (retracting)
        {
            currentLength -= retractSpeed * Time.deltaTime;
            if (currentLength <= 0f)
            {
                currentLength = 0f;
                retracting = false;
            }
        }

        UpdateTongueVisual();
    }

    private void UpdateTongueVisual()
    {
        tongueMesh.localScale = new Vector3(tongueWidth, currentLength / 2f, tongueWidth);
        tongueMesh.localPosition = new Vector3(0, 0, currentLength / 2f);
    }
}

