using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class HyperToonSettings : ScriptableObject
{
    
}

public static class HyperToonInfo {
    private const string Version = "0.10.1";
    private const string ReleaseDate = "06.12.2023";
    private const string Message = "by evets.";
    private const string Notes = "created for HyperStars, or any subsequent games we end up making.";
    
    public static readonly string FullInfo = $"{Message} version: {Version} ({ReleaseDate})\n{Notes}";
}
