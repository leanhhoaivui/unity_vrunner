using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    
    private const string SAVE_FILE_NAME = "savegame.json";
    private const string PREFS_HIGH_SCORE = "HighScore";
    private const string PREFS_TOTAL_COINS = "TotalCoins";
    
    [SerializeField] private bool useJSON = true; // true = JSON, false = PlayerPrefs
    
    private SaveData currentSave;
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    
    public SaveData CurrentSave => currentSave;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        LoadGame();
    }
    
    /// <summary>
    /// Load game data
    /// </summary>
    public void LoadGame()
    {
        if (useJSON)
        {
            LoadFromJSON();
        }
        else
        {
            LoadFromPlayerPrefs();
        }
        
        Debug.Log($"Game loaded. High Score: {currentSave.highScore}");
    }
    
    /// <summary>
    /// Save game data
    /// </summary>
    public void SaveGame()
    {
        if (useJSON)
        {
            SaveToJSON();
        }
        else
        {
            SaveToPlayerPrefs();
        }
        
        Debug.Log("Game saved!");
    }
    
    #region JSON Save/Load
    
    private void LoadFromJSON()
    {
        if (File.Exists(SaveFilePath))
        {
            try
            {
                string json = File.ReadAllText(SaveFilePath);
                currentSave = JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load save file: {e.Message}");
                currentSave = new SaveData();
            }
        }
        else
        {
            currentSave = new SaveData();
        }
    }
    
    private void SaveToJSON()
    {
        try
        {
            string json = JsonUtility.ToJson(currentSave, true);
            File.WriteAllText(SaveFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }
    
    #endregion
    
    #region PlayerPrefs Save/Load
    
    private void LoadFromPlayerPrefs()
    {
        currentSave = new SaveData
        {
            highScore = PlayerPrefs.GetInt(PREFS_HIGH_SCORE, 0),
            totalCoins = PlayerPrefs.GetInt(PREFS_TOTAL_COINS, 0),
            gamesPlayed = PlayerPrefs.GetInt("GamesPlayed", 0),
            totalDistance = PlayerPrefs.GetFloat("TotalDistance", 0f),
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f),
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f),
            vibrationEnabled = PlayerPrefs.GetInt("Vibration", 1) == 1
        };
    }
    
    private void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt(PREFS_HIGH_SCORE, currentSave.highScore);
        PlayerPrefs.SetInt(PREFS_TOTAL_COINS, currentSave.totalCoins);
        PlayerPrefs.SetInt("GamesPlayed", currentSave.gamesPlayed);
        PlayerPrefs.SetFloat("TotalDistance", currentSave.totalDistance);
        PlayerPrefs.SetFloat("MusicVolume", currentSave.musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", currentSave.sfxVolume);
        PlayerPrefs.SetInt("Vibration", currentSave.vibrationEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    #endregion
    
    #region High Score
    
    public int GetHighScore()
    {
        return currentSave.highScore;
    }
    
    public bool IsNewHighScore(int score)
    {
        return score > currentSave.highScore;
    }
    
    public void SetHighScore(int score)
    {
        if (score > currentSave.highScore)
        {
            currentSave.highScore = score;
            SaveGame();
            Debug.Log($"New high score: {score}!");
        }
    }
    
    #endregion
    
    #region Stats
    
    public void AddCoins(int amount)
    {
        currentSave.totalCoins += amount;
        SaveGame();
    }
    
    public void AddDistance(float distance)
    {
        currentSave.totalDistance += distance;
        SaveGame();
    }
    
    public void IncrementGamesPlayed()
    {
        currentSave.gamesPlayed++;
        SaveGame();
    }
    
    #endregion
    
    #region Settings
    
    public void SetMusicVolume(float volume)
    {
        currentSave.musicVolume = Mathf.Clamp01(volume);
        SaveGame();
    }
    
    public void SetSFXVolume(float volume)
    {
        currentSave.sfxVolume = Mathf.Clamp01(volume);
        SaveGame();
    }
    
    public void SetVibration(bool enabled)
    {
        currentSave.vibrationEnabled = enabled;
        SaveGame();
    }
    
    #endregion
    
    /// <summary>
    /// Reset all save data (for testing)
    /// </summary>
    public void ResetSave()
    {
        currentSave = new SaveData();
        
        if (useJSON)
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }
        }
        else
        {
            PlayerPrefs.DeleteAll();
        }
        
        Debug.Log("Save data reset!");
    }
}