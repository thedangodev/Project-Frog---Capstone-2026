using UnityEngine;

// Place on any ground/floor object to tag what surface it represents. The enum names must exactly match the labels of the "Surface" labeled parameter in FMOD Studio (case-sensitive). -E.M

public class SurfaceTypeManager : MonoBehaviour
{
    public enum Surface
    {
        Ground = 0,
        Lilypads = 1,
        Mud = 2,
        Stone = 3
    }

    [SerializeField] private Surface surface = Surface.Ground;

    public Surface CurrentSurface => surface;
    public string SurfaceLabel => surface.ToString();
    public int SurfaceIndex => (int)surface;
}