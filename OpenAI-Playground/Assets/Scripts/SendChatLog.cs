// og author: https://www.youtube.com/watch?v=2-tUwIQmBNE

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SendChatLog : MonoBehaviour
{
    string URL = "https://docs.google.com/forms/u/1/d/e/1FAIpQLSeKel7d5BLaLVM5R4q28GSPl1qYRPdJ0MKx_YEsNgwZnJscmw/formResponse";

    public void Send(string chatLog) => StartCoroutine(Post(chatLog));

    IEnumerator Post(string s1)
    {
        WWWForm form = new WWWForm();
        form.AddField("entry.86581814", s1);
        UnityWebRequest www = UnityWebRequest.Post(URL, form);
        yield return www.SendWebRequest();
    }
}