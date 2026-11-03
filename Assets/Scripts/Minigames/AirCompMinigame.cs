using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AirCompMinigame : MonoBehaviour, IMinigame
{
    [SerializeField] private Transform buttonParent;
    [SerializeField] private GameObject bar;
    [SerializeField] private List<LayoutElement> sections = new List<LayoutElement>();

    [SerializeField] private int numCorrect;
    private MinigameManager minigameManager;
    private List<int> percentForBar = new List<int>();
    public List<int> correctOrder = new List<int>();

    public List<Color> normalColors = new List<Color>();

    public List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();



    void Awake()
    {
        if (minigameManager == null)
        {
            GameObject tmp = GameObject.Find("Minigame Manager");
            minigameManager = tmp.GetComponent<MinigameManager>();
        }
    }

    public void StartMinigame()
    {
        bar.SetActive(true);

        numCorrect = 0;
        percentForBar.Clear();
        correctOrder.Clear();

        ResetColors();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CreateBarValues();

        BarGenerator();
        CreateSortedList();
        //Debug.Log(string.Join(", ", percentForBar));
    }

    void CreateBarValues()
    {
        int remaining = 100;
        int currentValue = 0;

        percentForBar.Add(currentValue);

        for (int i = 0; i < 8; i++)
        {
            if (i == 7)
            {
                percentForBar.Add(100);
                break;
            }
            int cutsLeft = 8 - i;

            int min = 5;
            int max = Mathf.Min(22, remaining - (cutsLeft - 1) * 3); //maximum you can use while leaving 3 units for each remaining cut

            int cut = Random.Range(min, max + 1);

            currentValue += cut;
            remaining -= cut;

            percentForBar.Add(currentValue);
        }
    }

    public void EndMinigame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        bar.SetActive(false);
    }

    void CreateSortedList()
    {
        for (int i = 0; i < percentForBar.Count - 1; i++)
        {
            correctOrder.Add(i);
        }

        for (int i = 0; i < correctOrder.Count; i++)
        {
            for (int j = 0; j < correctOrder.Count - 1; j++)
            {
                int diffA = percentForBar[correctOrder[j] + 1] - percentForBar[correctOrder[j]];
                int diffB = percentForBar[correctOrder[j + 1] + 1] - percentForBar[correctOrder[j + 1]];

                if (diffA > diffB)
                {
                    int temp = correctOrder[j];
                    correctOrder[j] = correctOrder[j + 1];
                    correctOrder[j + 1] = temp;
                }
            }
        }

        correctOrder.Reverse();
    }

    void BarGenerator()
    {
        for (int i = 0; i < sections.Count; i++)
        {

            int sectionWidth = percentForBar[i + 1] - percentForBar[i];

            //Debug.Log(sectionWidth);

            sections[i].flexibleWidth = sectionWidth;
        }
    }

    public void CheckIfCorrect()
    {
        //this is a beautiful thing i learned in casual from one of my programmers, it finds the button so you can use it's name as a value
        GameObject myButtonGameObject = EventSystem.current.currentSelectedGameObject;
        if (myButtonGameObject == null)
        {
            Debug.Log("Unable to find GameObject.");
            return;
        }
        Button myButton = myButtonGameObject.GetComponent<Button>();

        if (myButtonGameObject.name == correctOrder[numCorrect].ToString())
        {
            Debug.Log("Correct Element");
            buttons[correctOrder[numCorrect]].image.color = Color.green;
            numCorrect++;
        }

        else
        {
            Debug.Log("Wrong Element");
            ResetColors();
            numCorrect = 0;
        }

        if (numCorrect == 8)
        {
            Debug.Log("Correct Air Composition");
            minigameManager.EndCurrentMinigame();
        }
    }

    void ResetColors()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].image.color = normalColors[i];
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.name == "Player Body")
        {
            minigameManager.StartMinigame(this);
        }
    }

    void OnTriggerExit(Collider collider)
    {
        minigameManager.EndCurrentMinigame();
    }
}
