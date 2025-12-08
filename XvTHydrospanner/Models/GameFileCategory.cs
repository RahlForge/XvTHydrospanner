using System.Collections.Generic;

namespace XvTHydrospanner.Models
{
    /// <summary>
    /// Represents a game file directory category
    /// </summary>
    public class GameFileCategory
    {
        public string Name { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> FileExtensions { get; set; } = new();
        
        public static List<GameFileCategory> GetDefaultCategories()
        {
            return new List<GameFileCategory>
            {
                new GameFileCategory
                {
                    Name = "Battle Missions",
                    RelativePath = "Battle",
                    Description = "Battle mode mission files",
                    FileExtensions = new List<string> { ".LST", ".TIE" }
                },
                new GameFileCategory
                {
                    Name = "Combat Missions",
                    RelativePath = "Combat",
                    Description = "Single combat mission files",
                    FileExtensions = new List<string> { ".LST", ".TIE" }
                },
                new GameFileCategory
                {
                    Name = "Balance of Power - Battle",
                    RelativePath = "BalanceOfPower/BATTLE",
                    Description = "Balance of Power expansion battle missions",
                    FileExtensions = new List<string> { ".LST", ".TIE" }
                },
                new GameFileCategory
                {
                    Name = "Balance of Power - Campaign",
                    RelativePath = "BalanceOfPower/CAMPAIGN",
                    Description = "Balance of Power campaign missions",
                    FileExtensions = new List<string> { ".LST", ".TIE" }
                },
                new GameFileCategory
                {
                    Name = "Balance of Power - Training",
                    RelativePath = "BalanceOfPower/TRAIN",
                    Description = "Balance of Power training missions",
                    FileExtensions = new List<string> { ".LST", ".TIE" }
                },
                new GameFileCategory
                {
                    Name = "Balance of Power - Melee",
                    RelativePath = "BalanceOfPower/MELEE",
                    Description = "Balance of Power melee missions",
                    FileExtensions = new List<string> { ".LST", ".TIE" }
                },
                new GameFileCategory
                {
                    Name = "Balance of Power - Tournament",
                    RelativePath = "BalanceOfPower/TOURN",
                    Description = "Balance of Power tournament missions",
                    FileExtensions = new List<string> { ".LST", ".TIE" }
                },
                new GameFileCategory
                {
                    Name = "Graphics - 320x200",
                    RelativePath = "cp320",
                    Description = "Graphics and cockpit files for 320x200 resolution",
                    FileExtensions = new List<string> { ".LFD", ".INT", ".PNL" }
                },
                new GameFileCategory
                {
                    Name = "Graphics - 640x480",
                    RelativePath = "cp640",
                    Description = "Graphics and cockpit files for 640x480 resolution",
                    FileExtensions = new List<string> { ".LFD", ".INT", ".PNL" }
                },
                new GameFileCategory
                {
                    Name = "Movies - A",
                    RelativePath = "Amovie",
                    Description = "Movie files (A series)",
                    FileExtensions = new List<string> { ".WRK" }
                },
                new GameFileCategory
                {
                    Name = "Movies - B",
                    RelativePath = "Bmovie",
                    Description = "Movie files (B series)",
                    FileExtensions = new List<string> { ".WRK" }
                },
                new GameFileCategory
                {
                    Name = "Music",
                    RelativePath = "Music",
                    Description = "Music files",
                    FileExtensions = new List<string> { ".VOC", ".WAV" }
                },
                new GameFileCategory
                {
                    Name = "Sound Effects",
                    RelativePath = "wave",
                    Description = "Sound effect files",
                    FileExtensions = new List<string> { ".WAV", ".VOC" }
                },
                new GameFileCategory
                {
                    Name = "Resources",
                    RelativePath = "resource",
                    Description = "Game resource files",
                    FileExtensions = new List<string> { ".LFD" }
                },
                new GameFileCategory
                {
                    Name = "Configuration",
                    RelativePath = "",
                    Description = "Game configuration files",
                    FileExtensions = new List<string> { ".CFG", ".TXT" }
                }
            };
        }
    }
}
