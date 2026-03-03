using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    [Header("Names")]
    public string carName;
    public string mapName;

    [Header("Obstacles")]
    public int staticCount;
    public int dynamicCount;

    [Header("Counters")]
    public int episodeCount;
    public int checkpointCount;
    public int pos;

    [Header("Steps")]
    public float stepsTotal;
    public float stepsAvg;
    public List<float> stepsPerEpisode;

    [Header("Rewards")]
    public float rewardTotal;
    public float rewardAvg;
    public List<float> rewardPerEpisode;

    [Header("Speeds")]
    public float speedMax;
    public float speed;
    public float speedTotal;
    public List<float> speedAvgs;
    public float speedTotalAllEpisodes;
    public float speedAvgAllEpisodes;

    [Header("Lap Times")]
    public float lapStart;
    public float lapFast;
    public float lapSlow;
    public float lapAvg;
    public float lapTotal;
    public List<float> laps;

    [Header("Collision Triggers")]
    public int colTriggers;
    public int colTriggersTotal;
    public float colTriggersAvg;
    public List<float> colTriggersPerEpisode;

    [Header("Collision Stays")]
    public int colStays;
    public float colStaysTotal;
    public float colStaysAvg;
    public List<float> colStaysPerEpisode;
}
