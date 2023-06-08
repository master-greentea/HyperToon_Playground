using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class HyperToonSettings : ScriptableObject
{
    
}

public static class HyperToonInfo {
    private const string Version = "0.7.2";
    private const string ReleaseDate = "06.07.2023";
    private const string Message = "by evets.";
    private const string Notes = "added pattern shader";
    
    public static readonly string FullInfo = $"{Message} version: {Version} ({ReleaseDate})\n{Notes}";
}
