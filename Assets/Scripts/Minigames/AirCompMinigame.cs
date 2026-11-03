using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AirCompMinigame : MonoBehaviour, IMinigame
{
    [SerializeField] private GameObject bar;
    [SerializeField] private List<LayoutElement> sections = new List<LayoutElement>();
    private MinigameManager minigameManager;
    public List<int> percentForBar = new List<int>();

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

        percentForBar.Clear();

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

        BarGenerator();
        //Debug.Log(string.Join(", ", percentForBar));
    }

    public void EndMinigame()
    {
        bar.SetActive(false);
    }

    void BarGenerator()
    {
        for (int i = 0; i < sections.Count; i++)
        {

            int sectionWidth = percentForBar[i + 1] - percentForBar[i];

            Debug.Log(sectionWidth);

            sections[i].flexibleWidth = sectionWidth;
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
