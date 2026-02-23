using UnityEngine;
using TMPro;

public class PinCounter : MonoBehaviour
{
    private FallingPin[] pins;
    public int fallenCount = 0;

    public TextMeshProUGUI scoreText;

    void Start()
    {
        // récupère toutes les quilles sous BowlingAlley (une seule fois)
        pins = GetComponentsInChildren<FallingPin>();
    }

    void Update()
    {
        fallenCount = 0;

        foreach (var pin in pins)
        {
            if (pin.isFallen)
                fallenCount++;
        }
            scoreText.text = fallenCount.ToString();
    }
}