using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }
    public Stats[] stats;
    public int lapsToEpisodeEnd = 1;

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

        // List of stats classes
        // These store stats for all agents in scene
        Transform carFolder = GameObject.FindWithTag("folder_cars").transform;
        stats = new Stats[carFolder.childCount];
        for (int i = 0; i < carFolder.childCount; i++)
        {
            stats[i] = carFolder.GetChild(i).GetComponent<Stats>();
            stats[i].carName = carFolder.GetChild(i).gameObject.name.Split("_")[1];
            (float maxMove, float maxTurn, float maxBrake) = stats[i].gameObject.GetComponent<CarController>().GetStatsMax();
            stats[i].speedMax = maxMove;
        }
        ResetStats();
    }

    public void SetMap(string newMap = "mapName")
    {
        for (int i = 0; i < stats.Length; i++) stats[i].mapName = newMap;
    }

    public void SetObstacles(int index, int staticObs = 0, int dynamicObs = 0)
    {
        stats[index].staticCount = staticObs;
        stats[index].dynamicCount = dynamicObs;
    }

    public void SetEpisode(int index, int amount = 1, bool doSet = false)
    {
        stats[index].episodeCount = doSet ? amount : stats[index].episodeCount + amount;
    }

    public void AddSteps(int index, int amount = 1)
    {
        stats[index].stepsTotal += amount;
        stats[index].stepsPerEpisode.Add(amount);
        stats[index].stepsAvg = stats[index].stepsTotal / stats[index].stepsPerEpisode.Count;
    }

    public void AddEpisodeReward(int index, float amount)
    {
        stats[index].rewardTotal += amount;
        stats[index].rewardPerEpisode.Add(amount);
        stats[index].rewardAvg = stats[index].rewardTotal / stats[index].rewardPerEpisode.Count;
    }

    public void AddColTrigger(int index, int amount = 1, bool episodeEnd = false)
    {
        if (episodeEnd)
        {
            stats[index].colTriggersTotal += stats[index].colTriggers;
            stats[index].colTriggersPerEpisode.Add(stats[index].colTriggers);
            stats[index].colTriggersAvg = stats[index].colTriggersTotal / (float)stats[index].colTriggersPerEpisode.Count;
        }
        else
        {
            stats[index].colTriggers += amount;
        }
    }

    public void AddColStay(int index, int amount = 1, bool episodeEnd = false)
    {
        if (episodeEnd)
        {
            stats[index].colStaysTotal += stats[index].colStays;
            stats[index].colStaysPerEpisode.Add(stats[index].colStays);
            stats[index].colStaysAvg = stats[index].colStaysTotal / (float)stats[index].colStaysPerEpisode.Count;
        }
        else
        {
            stats[index].colStays += amount;
        }
    }

    void SetEpisodeSpeed(int index, int steps)
    {
        float speedAvgThisEpisode = (stats[index].speedTotal * stats[index].speedMax) / (float)steps;
        stats[index].speedAvgs.Add(speedAvgThisEpisode);
        stats[index].speedTotalAllEpisodes += speedAvgThisEpisode;
        stats[index].speedAvgAllEpisodes = stats[index].speedTotalAllEpisodes / (float)stats[index].speedAvgs.Count;
        stats[index].speed = 0f;
    }

    public void HitCheckpoint(int index, int amount)
    {
        stats[index].checkpointCount = amount;
    }

    public bool SetLapEnd(int index)
    {
        float duration = Time.time - stats[index].lapStart;
        stats[index].lapTotal += duration;
        stats[index].laps.Add(duration);
        stats[index].lapAvg = stats[index].lapTotal / stats[index].laps.Count;
        stats[index].lapFast = stats[index].laps.Min();
        stats[index].lapSlow = stats[index].laps.Max();
        stats[index].lapStart = Time.time;
        return stats[index].laps.Count >= lapsToEpisodeEnd ? true : false;
    }

    public void ResetStats(int index = -1)
    {
        if (index == -1)
        {
            for (int i = 0; i < stats.Length; i++) DoReset(i);
        }
        else
        {
            DoReset(index);
        }
    }

    void DoReset(int index)
    {
        ResetEpisodeValues(index);

        stats[index].episodeCount = 0;
        stats[index].pos = 0;

        stats[index].stepsAvg = 0f;
        stats[index].stepsPerEpisode.Clear();

        stats[index].rewardAvg = 0f;
        stats[index].rewardPerEpisode.Clear();

        stats[index].speed = 0f;
        stats[index].speedAvgs.Clear();
        stats[index].speedTotalAllEpisodes = 0f;
        stats[index].speedAvgAllEpisodes = 0f;

        stats[index].lapTotal = 0f;
        stats[index].laps.Clear();
        stats[index].lapAvg = 0f;
        stats[index].lapFast = 0f;
        stats[index].lapSlow = 0f;

        stats[index].colTriggersPerEpisode.Clear();
        stats[index].colTriggersAvg = 0f;
        stats[index].colTriggersTotal = 0;

        stats[index].colStaysPerEpisode.Clear();
        stats[index].colStaysAvg = 0f;
        stats[index].colStaysTotal = 0;
    }

    public void ResetEpisodeValues(int index)
    {
        // At the end of an episode, reset some stats, like counters
        stats[index].stepsTotal = 0;
        stats[index].rewardTotal = 0f;
        stats[index].speedTotal = 0f;
        stats[index].lapStart = Time.time;
        stats[index].colTriggers = 0;
        stats[index].colStays = 0;
    }

    public void EndEpisode(int index, int steps, float reward)
    {
        // Call these methods upon episode end
        SetEpisode(index);
        AddSteps(index, steps);
        AddEpisodeReward(index, reward);
        AddColTrigger(index, 0, true);
        AddColStay(index, 0, true);
        SetEpisodeSpeed(index, steps);
        ResetEpisodeValues(index);
    }

    public void EndStep(int index, float speed)
    {
        stats[index].speed = speed * stats[index].speedMax;
        stats[index].speedTotal += speed;
    }

    void OnApplicationQuit()
    {
        // When training/testing is over
        string folderPath = GetFolderPath();
        ExportValues(folderPath);
        ExportLists(folderPath);
    }

    string GetFolderPath()
    {
        // Manage directories
        string folderName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string folderPath = Directory.GetParent(Application.dataPath).ToString();
        folderPath = Path.Combine(folderPath, "data");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        folderPath = Path.Combine(folderPath, folderName);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    void ExportValues(string folderPath)
    {
        // Store per agent summary in one file, each row is one agent
        StringBuilder exportString = new StringBuilder();
        exportString.AppendLine(
            "Name," +
            "Map," +
            "StaticObs," +
            "DynamicObs," +
            "Episodes," +
            "StepsTotal," +
            "StepsAvg," +
            "RwdTotal," +
            "RwdAvg," +
            "SpeedAvg," +
            "LapAvg," +
            "LapFast," +
            "LapSlow," +
            "ColTriggersAvg," +
            "ColStaysAvg"
        );
        foreach (Stats s in stats)
        {
            exportString.AppendLine(
                $"{s.carName}," +
                $"{s.mapName}," +
                $"{s.staticCount}," +
                $"{s.dynamicCount}," +
                $"{s.episodeCount}," +
                $"{s.stepsTotal}," +
                $"{s.stepsAvg}," +
                $"{s.rewardTotal}," +
                $"{s.rewardAvg}," +
                $"{s.speedAvgAllEpisodes}," +
                $"{s.lapAvg}," +
                $"{s.lapFast}," +
                $"{s.lapSlow}," +
                $"{s.colTriggersAvg}," +
                $"{s.colStaysAvg}"
             );
        }
        string fileName = "_Summary.csv";
        string filePath = Path.Combine(folderPath, fileName);
        File.WriteAllText(filePath, exportString.ToString());
    }

    void ExportLists(string folderPath)
    {
        // Store per episode stats in separate files for each agent
        int index = 0;
        foreach (Stats s in stats)
        {
            StringBuilder exportString = new StringBuilder();
            exportString.AppendLine(
                "Episode," +
                "Steps," +
                "Reward," +
                "SpeedAvg," +
                "ColTriggers," +
                "ColStays"
            );
            for (int i = 0; i < s.episodeCount; i++)
            {
                exportString.AppendLine(
                    $"{i}," +
                    $"{s.stepsPerEpisode[i]}," +
                    $"{s.rewardPerEpisode[i]}," +
                    $"{s.speedAvgs[i]}," +
                    $"{s.colTriggersPerEpisode[i]}," +
                    $"{s.colStaysPerEpisode[i]}"
                );
            }
            string fileName = $"{index}_{s.carName}_{s.gameObject.GetComponent<CarAgent>().GetModelName()}.csv";
            string filePath = Path.Combine(folderPath, fileName);
            File.WriteAllText(filePath, exportString.ToString());
            index++;
        }
    }
}
