using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainPageScript : MonoBehaviour
{
    public Button startbutton;
    public Button thaibutton;
    public Button engbutton;
    public GameObject thaiframe;
    public GameObject engframe;
    public bool thaiSelected = false;

    // Start is called before the first frame update
    void Start()
    {
        thaibutton.onClick.AddListener(setToThaiLanguage);
        engbutton.onClick.AddListener(setToEngLanguage);
        startbutton.onClick.AddListener(moveToMainProgram);

        if (PlayerPrefs.GetString("isThai") == "True"){
            setToThaiLanguage();
        }
    }

    private void setToThaiLanguage(){
        switchLanguage(true);
    }

    private void setToEngLanguage(){
        switchLanguage(false);
    }

    private void switchLanguage(bool isThai){
        thaiSelected = isThai;
        if (isThai) {
            thaiframe.SetActive(true);
            engframe.SetActive(false);
        } else {
            thaiframe.SetActive(false);
            engframe.SetActive(true);
        }
        Debug.Log("" + thaiSelected);
    }

    private void moveToMainProgram(){
        PlayerPrefs.SetString("isThai", "" + thaiSelected);
        SceneManager.LoadScene("Stateseparate", LoadSceneMode.Single);
    }
}
