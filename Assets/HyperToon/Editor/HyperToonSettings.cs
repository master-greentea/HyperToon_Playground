using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class HyperToonSettings : ScriptableObject
{
    
}

public static class HyperToonInfo {
    private const string Version = "0.8.0";
    private const string ReleaseDate = "06.09.2023";
    private const string Message = "by evets.";
    private const string Notes = "added skybox";
    
    public static readonly string FullInfo = $"{Message} version: {Version} ({ReleaseDate})\n{Notes}";
}
