using UnityEngine;
using System.Collections;

public class QuitButton : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void quit()
    {
        Debug.Log("quit");
        Application.Quit();
    }
}
