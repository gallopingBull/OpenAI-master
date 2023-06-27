using UnityEngine;

public class ToggleGameObject : MonoBehaviour
{
    // generate new method that toggls the game object active state
    public void ToggleActiveState()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
