using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject loadingBar;
    public Slider currentSlider;
    public Slider overallSlider;
    public Text processDetail;
    private int totalProcess = 1;
    public int completedProcess = 0;

    // Start is called before the first frame update
    public void initialValue()
    {
        currentSlider.value = 0.0f;
        overallSlider.value = 0.0f;
    }

    public void setTotalProcess(int process_amount)
    {
        totalProcess = process_amount;
    }

    public int getCompletedProcess(){
        return completedProcess;
    }

    public void triggerCompleteProcess(){
        completedProcess++;
        triggerUpdateOverallProcess();
    }

    public void triggerUpdateOverallProcess(){
        overallSlider.value = (float)completedProcess / totalProcess;
    }

    public void updateCurrentProgress(float completion_rate){
        currentSlider.value = completion_rate / 1.0f;
    }

    public void updateProcessDetail(string detail)
    {
        processDetail.text = detail;
    }

    public void setupActivation(bool active){
        loadingBar.SetActive(active);
    }
}
