//Attach to a empty game object that is attached to the player. Position this game object where the tongue should extend from. Name it TongueRoot for some clarity.
using UnityEngine;

public class PlayerTongueAttack : MonoBehaviour
{
    [SerializeField] private Transform tongueMesh; //Have Tongue mesh be a child of TongueRoot.
    [SerializeField] private float maxLength = 10f;
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private float tongueWidth = 0.3f;

    private float currentLength = 0f;
    public bool extending = false; //keep public AttackScript uses this to stop player shooting when using Tongue.
    public bool retracting = false; //keep public AttackScript uses this to stop player shooting when using Tongue.

    public bool IsActive => extending || retracting;

    public System.Action OnTongueFinished;

    public void BeginTongue()
    {
        if (retracting) return;
        extending = true;
        retracting = false;
    }

    public void EndTongue() //Call this when hitting an enemy or healing bug to instantly begin retracting. 
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

                OnTongueFinished?.Invoke();
            }
        }

        UpdateTongueVisual();
    }

    private void UpdateTongueVisual()
    {
        tongueMesh.localScale = new Vector3(tongueWidth, currentLength / 2f, tongueWidth);
        tongueMesh.localPosition = new Vector3(0, 0, currentLength / 2f); //MAKE THIS 0, 0, 0 WHEN FINAL MESH IS ADDED, And make sure the pivot for that mech isn't dead center.
    }
}

