using UnityEngine;

public class CowClick : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown()
    {
        Debug.Log("Cow clicked");
        
        // Find the currently occupying vessel
        DragMilkVessel[] allVessels = FindObjectsByType<DragMilkVessel>(FindObjectsSortMode.None);
        foreach (DragMilkVessel vessel in allVessels)
        {
            if (vessel.IsOccupyingTarget())
            {
                vessel.OnCowClicked();
                return;
            }
        }
        
        Debug.Log("No vessel is currently at the target position");
    }
}
