using Assets.Scripts.DDA;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Logging
{
    public class LogMaster : MonoBehaviour
    {
        public static LogMaster Instance { get; private set; }

        [Header("Log info")]
        [SerializeField] private string fileName = "DdaValues";
        [SerializeField] private string folderName = "Logs";

        private List<DdaLogData> ddaLogData = new List<DdaLogData>();

        #region Functions where this gets data
        /// <summary>
        /// Remembers what time the difficulty was adjusted and to what
        /// </summary>
        /// <param name="time">The Time.time this difficulty was reached</param>
        /// <param name="difficulty">What difficulty the game currently is on</param>
        public void AddDdaLogData(float time, float difficulty)
        {
            AddDdaLogData(new DdaLogData(time, difficulty));
        }
        public void AddDdaLogData(DdaLogData newLogData)
        {
            ddaLogData.Add(newLogData);
        }
        #endregion

        private void Start()
        {
            // Singleton this
            if (Instance != null)
            {
                Debug.LogWarning($"Multiple instances of LogMaster, selfdestructing {name}");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

#if UNITY_EDITOR
        /// <summary>
        /// For testing, press M to save csv file at any point
        /// </summary>
        private void Update()
        {
    #if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.mKey.wasPressedThisFrame)
                    SaveLogAsCsv();
            }
    #else
            if (Input.GetKeyDown(KeyCode.M))
                SaveLogAsCsv();
    #endif
        }
#endif

        /// <summary>
        /// Saves a log when the scene is unloaded or the logger is elsehow disabled
        /// </summary>
        private void OnDisable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                SaveLogAsCsv();
        }

        /// <summary>
        /// Saves the log as a csv.
        /// To be called at scene end.
        /// </summary>
        public void SaveLogAsCsv()
        {
            //Create file
            int logNum = 0;

            string myDataPath = Application.dataPath;
            // Go from last to first character
            for (int i = myDataPath.Length - 1; i >= 0; i--)
            {
                // Remove last / or \ in the filepath
                if (myDataPath[i] == '/' || myDataPath[i] == '\\')
                {
                    myDataPath = myDataPath.Remove(i);
                    break;
                }
            }

            // Create folder if it doesn't already exist
            // CreateDirectory is smartypants and doesn't create something that already exists
            string folderPath = Path.Combine(myDataPath, folderName);
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, fileName + "_" + logNum + ".csv");

            // If file already exists, count up
            while (File.Exists(filePath))
            {
                logNum++;
                filePath = Path.Combine(folderPath, fileName + "_" + logNum + ".csv");
            }

            Debug.Log($"Logging at {filePath}. {ddaLogData.Count} ddaLogs");

            // Create text element
            StreamWriter writer = File.CreateText(filePath);

            // Write titles
            writer.WriteLine("Time;Difficulty");

            // Write out the data
            for (int i = 0; i < ddaLogData.Count; i++)
            {
                writer.WriteLine($"{ddaLogData[i].time:N2};{ddaLogData[i].difficulty:N2}");
            }

            // Close the writer
            writer.Close();
        }
    }

    /// <summary>
    /// Difficulty by time
    /// </summary>
    public struct DdaLogData
    {
        public float time;
        public float difficulty;

        public DdaLogData(float time, float difficulty)
        {
            this.time = time;
            this.difficulty = difficulty;
        }
    }
}
