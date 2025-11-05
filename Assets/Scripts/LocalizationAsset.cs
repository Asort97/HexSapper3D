using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationAsset", menuName = "")]
public class LocalizationAsset : ScriptableObject
{
    public Localization[] localizations;
    
    [Serializable]
    public struct Localization
    {
        public string Language;
        public Dict[] localizations;
    }

    [Serializable]
    public struct Dict
    {
        public string key;
        public string value;
    }

}