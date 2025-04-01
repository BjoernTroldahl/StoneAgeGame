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
        DragMilkVessel vessel = FindFirstObjectByType<DragMilkVessel>();
        if (vessel != null)
        {
            vessel.OnCowClicked();
        }
    }
}
