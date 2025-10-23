using System;
using UnityEngine;

[Serializable]
public struct Rank
{
    public string rankName;
    public int score;
}

[CreateAssetMenu(fileName = "RankHierarchy", menuName = "Hierarchy")]
public class RankHierarchy : ScriptableObject
{
    public Rank[] ranks;
}