using UnityEngine;
using TMPro;   // 👈 para TextMeshPro

public class GameTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private bool countDown = false;
    [SerializeField] private float startTime = 300f; // segundos si es countdown (5 min)

    private float elapsedTime = 0f;
    private bool isRunning = true;

    void Start()
    {
        if (countDown)
            elapsedTime = startTime;
        else
            elapsedTime = 0f;
    }

    void Update()
    {
        if (!isRunning) return;

        if (countDown)
        {
            elapsedTime -= Time.deltaTime;
            if (elapsedTime <= 0f)
            {
                elapsedTime = 0f;
                isRunning = false;
            }
        }
        else
        {
            elapsedTime += Time.deltaTime;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // --- Métodos públicos ---
    public void PauseTimer() => isRunning = false;
    public void ResumeTimer() => isRunning = true;
    public void ResetTimer()
    {
        elapsedTime = countDown ? startTime : 0f;
        isRunning = true;
    }
}
