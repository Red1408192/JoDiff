using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace JoDiff.Models
{
    public class GameData
    {
        private GameData(GameEnum gameId, string baseGamePath)
        {
            GameId = gameId;
            BaseGamePath = baseGamePath;
            GameObject = ParseGameFolder(BaseGamePath);
        }

        public GameEnum GameId { get; private set; }
        public string BaseGamePath { get; private set; }
        public GameObject GameObject { get; private set; }

        private GameObject ParseGameFolder(string baseGamePath)
        {
            var baseDirectoryFiles = Directory.EnumerateFileSystemEntries(baseGamePath);
            if(!baseDirectoryFiles.Any(x => x.EndsWith(JoDiffCostants.Folders.GameFolder))) throw new InvalidDataException("Invalid Game Folder");
            else
            {
                var gameDir = Path.Combine(baseGamePath, JoDiffCostants.Folders.GameFolder);
                var gameDirectoryFiles = Directory.EnumerateFileSystemEntries(gameDir);
                if(!gameDirectoryFiles.Any(x => x.EndsWith(JoDiffCostants.Folders.CommonFolder))) throw new InvalidDataException("Invalid Game Folder");
                else
                {
                    var index = 0;
                    GameObject = GameObject.Parse(JoDiffCostants.Folders.CommonFolder, Path.Combine(gameDir, JoDiffCostants.Folders.CommonFolder), null, false, 0, ref index);
                }
            }

            return GameObject;
        }

        public static GameData GetGameData(GameEnum gameId, string baseGamePath)
        {
            //logic
            return new GameData(gameId, baseGamePath);
        }
    }
}


