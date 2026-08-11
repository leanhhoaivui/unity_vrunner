using System;

[Serializable]
public class SaveData
{
    public int highScore;
    public int totalCoins;
    public int gamesPlayed;
    public float totalDistance;
    
    // Settings
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool vibrationEnabled = true;
    
    public SaveData()
    {
        highScore = 0;
        totalCoins = 0;
        gamesPlayed = 0;
        totalDistance = 0f;
    }
}