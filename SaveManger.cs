using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    // Variables
    private static SaveManager instance;  // Static instance of SaveManager, using the Singleton pattern
    public SaveData saveData;
    private string saveDataDirectory;
    private static readonly object lockSaving = new object(); // used to threadsafe the saving function
    private static readonly object lockLoading = new object(); // used to threadsafe the loading function

    public static SaveManager Instance {// Singleton access method ensures that even across scene changes the data is always loaded and accessible
        get {
            if (instance == null) {
                GameObject obj = new GameObject("SaveManager");
                instance = obj.AddComponent<SaveManager>(); // Create the SaveManager if it doesn't exist
                DontDestroyOnLoad(obj); // Ensure it persists across scenes
            }
            return instance;
        }
    }

    private void Awake() { // awake funct that ensures that the savedata folder exists (acts as our constructor for monobehaviour classes)
        saveDataDirectory = Path.Combine(Application.dataPath, "SaveData");

        if (!Directory.Exists(saveDataDirectory)) {
            Directory.CreateDirectory(saveDataDirectory); // Create the SaveData folder
        }

        saveData = new SaveData(); // Initialize a new instance of the SaveData class
    }

    public bool SaveDataToSlot(string saveFolder) { // Save data to a specific save slot (Save1, Save2, etc.)
        lock (lockSaving) {
            saveData.FinishCurrQueueAndPause();
            // Ensuriing that the savefolder exists creates it if it does not
            string saveFolderPath = Path.Combine(saveDataDirectory, saveFolder);
            if (!Directory.Exists(saveFolderPath)) {
                Directory.CreateDirectory(saveFolderPath);
            }

            string filePath = Path.Combine(saveDataDirectory, saveFolder, "save.json");
            Debug.Log("testtt " + saveData);

            string json = JsonUtility.ToJson(saveData, true); // Serialize the saveData object (converts it to json)
            Debug.Log("Serialized data: " + json);
            File.WriteAllText(filePath, json); // Write data to file
            return true;
        }
    }

    public bool LoadDataFromSlot(string saveFolder) { // Load data from a specific save slot (Save1, Save2, etc.)
        lock (lockLoading) {
            saveData.FinishCurrQueueAndPause();
            string filePath = Path.Combine(saveDataDirectory, saveFolder, "save.json");

            if (File.Exists(filePath)) {
                string json = File.ReadAllText(filePath); // Read the file
                saveData = JsonUtility.FromJson<SaveData>(json); // Deserialize into saveData (convert json back into SaveData)
                return true;
            } else {
                return false;
            }
        }
    }

    public bool IsSaveDataFolderEmpty() { // Check to see if the base SaveData folder is empty (does it have saves in it or not)
        if (Directory.Exists(saveDataDirectory)) {
            // Getting a list of files and subdirectories
            string[] files = Directory.GetFiles(saveDataDirectory);
            string[] directories = Directory.GetDirectories(saveDataDirectory);

            return files.Length == 0 && directories.Length == 0; // If both files and directories arrays are empty, the folder is empty
        } else {
            return true;
        }
    }

    public int GetNumberOfSaveFiles() { // return the total number of save files in the SaveData folder
        // Get all directories inside the SaveData folder
        string[] directories = Directory.GetDirectories(saveDataDirectory);
        return directories.Length;
    }
}