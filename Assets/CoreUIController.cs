using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CoreUIController : MonoBehaviour
{
    public int MODE_MAP_SELECTION = 1;
    public int MODE_NORMAL_DOG_SELECTION = 2;
    public int MODE_ATTRACT_SELECTION = 3;
    private int screenMode = 1;
    public bool mapIsLocked = false;
    public bool startPreRegisterData = false;
    public GameObject startingScreen;
    public Button startScreenButton;
    public GameObject addNormalDogScreen;
    public GameObject populationQuantBox;
    public InputField dogQuantity;
    public Button cancelDogButton;
    public Button addDogButton;
    public Button useDefaultDataButton;
    public Button acceptDogPopButton;
    public Text errorDogInput;
    public Text allowDogInputStatus;
    public bool dogIsCancelledNotification = false;
    public bool dogIsAddedNotification = false;
    public bool allowAddDogObject = false;
    public bool useDefaultDogNotification = false;
    public bool usedDefaultData = false;

    public int getScreenMode(){
        return screenMode;
    }

    public void setScreenMode(int mode){
        screenMode = mode;
    }
    public void setupActivation(bool active){
        startingScreen.SetActive(active);
        startScreenButton.GetComponent<Button>().onClick.AddListener(moveToAddNormalDog);
        cancelDogButton.GetComponent<Button>().onClick.AddListener(cancelDogPopulation);
        useDefaultDataButton.GetComponent<Button>().onClick.AddListener(useDefaultDogData);
        addDogButton.GetComponent<Button>().onClick.AddListener(addDogPopulation);
        acceptDogPopButton.GetComponent<Button>().onClick.AddListener(moveToChangeParameters);
    }

    private void hideAllScreens(){
        startingScreen.SetActive(false);
        populationQuantBox.SetActive(false);
    }

    private void moveToAddNormalDog(){
        hideAllScreens();
        allowAddDogObject = true;
        mapIsLocked = true;
        addNormalDogScreen.SetActive(true);
    }

    public void initializeDogPopulationInput(){
        switchAllowInput(false);
        useDefaultDataButton.enabled = false;
        dogQuantity.GetComponent<InputField>().text = "";
        populationQuantBox.SetActive(true);
    }

    private void cancelDogPopulation(){
        populationQuantBox.SetActive(false);
        dogIsCancelledNotification = true;
        useDefaultDataButton.enabled = true;
    }

    private void addDogPopulation(){
        populationQuantBox.SetActive(false);
        dogIsAddedNotification = true;
        useDefaultDataButton.enabled = true;
    }

    public float getDogPopulation(){
        float value = 0;
        if (float.TryParse(dogQuantity.text, out value)) {
            return value;
        }
        return -1;
    }

    public void showDogErrorInput(string text){
        errorDogInput.GetComponent<Text>().text = text;
    }

    private void useDefaultDogData(){
        if (!usedDefaultData){
            usedDefaultData = true;
            useDefaultDogNotification = true;
        }
    }

    public void switchAllowInput(bool active){
        allowAddDogObject = active;
        if (active) {
            allowDogInputStatus.text = "Unlocked: you can add dog location";
        }
        else {
            allowDogInputStatus.text = "Locked: dog is not allowed to be added";
        }
    }

    public void moveToChangeParameters(){
        switchAllowInput(false);
        hideAllScreens();

    }
}
