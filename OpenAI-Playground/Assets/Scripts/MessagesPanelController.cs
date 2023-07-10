using UnityEngine;
using TMPro;
public class MessageController : MonoBehaviour
{

    public GameObject messagePrefab;
    public GameObject messagePanel;

    public void AddMessage(string message)
    {
        GameObject newMessage = Instantiate(messagePrefab, messagePanel.transform);
        newMessage.GetComponentInChildren<TMP_Text>().text = message;
    }
}
