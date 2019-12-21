using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class RestartButton : MonoBehaviour
{
    public Button restartButton;

    // Start is called before the first frame update
    void Start()
    {
        restartButton.onClick.AddListener(goBackToStartPage);
    }

    // Update is called once per frame
    private void goBackToStartPage(){
        SceneManager.LoadScene("StartingScreen", LoadSceneMode.Single);
    }
}
