using UnityEngine;
using TMPro;  

// Game Timer
public class GameTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    public float elapsedTime = 0f;
    private bool isRunning = true;

    void Start()
    {
        elapsedTime = 0f;
            
    }

    // Add the time passed
    void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateUI();
    }

    //Shows the time in the UI
    void UpdateUI()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // --- Métodos públicos ---
    public void PauseTimer() => isRunning = false;
}
