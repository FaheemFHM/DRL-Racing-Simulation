using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI : MonoBehaviour
{
    public static UI Instance { get; private set; }

    private TextMeshProUGUI nameText;
    private TextMeshProUGUI mapText;
    private TextMeshProUGUI lapText;
    private TextMeshProUGUI pointText;
    private TextMeshProUGUI speedText;
    private TextMeshProUGUI lapFastText;
    private TextMeshProUGUI lapSlowText;
    private TextMeshProUGUI lapAvgText;
    private TextMeshProUGUI lapCurText;
    [HideInInspector] public int carIndex;

    public void DoAwake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }

        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        nameText = texts[0];
        mapText = texts[1];
        lapText = texts[2];
        pointText = texts[3];
        speedText = texts[4];
        lapFastText = texts[5];
        lapSlowText = texts[6];
        lapAvgText = texts[7];
        lapCurText = texts[8];
    }

    public void SetMap(int index)
    {
        // Sets map string
        mapText.text = $"Map: {StatsManager.Instance.stats[index].mapName}";
    }

    public void SwitchCar(int index)
    {
        carIndex = index;
        SetName(index);
        SetPoint(index);
        SetLap(index);
        UpdateUI(index);
    }

    public void SetName(int index)
    {
        if (index != carIndex) return;
        nameText.text = $"Name: {StatsManager.Instance.stats[index].carName}";
    }

    public void SetPoint(int index)
    {
        if (index != carIndex) return;
        pointText.text = $"Point: {StatsManager.Instance.stats[index].checkpointCount}";
    }

    public void SetLap(int index)
    {
        // Set lap stats
        if (index != carIndex) return;
        lapText.text = $"Lap: {StatsManager.Instance.stats[index].laps.Count}";
        lapFastText.text = $"Fast: {FormatDuration(StatsManager.Instance.stats[index].lapFast)}";
        lapSlowText.text = $"Slow: {FormatDuration(StatsManager.Instance.stats[index].lapSlow)}";
        lapAvgText.text = $"Avg: {FormatDuration(StatsManager.Instance.stats[index].lapAvg)}";
        lapCurText.text = $"Cur: {FormatDuration(0f)}";
    }

    public void UpdateUI(int index)
    {
        // Update the ui for the correct car
        if (index != carIndex) return;
        float speedProp = Mathf.RoundToInt((StatsManager.Instance.stats[index].speed * 100) / StatsManager.Instance.stats[index].speedMax);
        speedText.text = $"Speed: {speedProp}%";
        lapCurText.text = $"Cur: {FormatDuration(Time.time - StatsManager.Instance.stats[index].lapStart)}";
    }

    public static string FormatDuration(float seconds)
    {
        // Turn seconds into hours, minutes and seconds
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:00}:{1:00}.{2:000}", t.Minutes, t.Seconds, t.Milliseconds);
    }
}
