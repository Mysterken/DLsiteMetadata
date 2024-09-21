using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace DLsiteMetadata;

public class DLsiteScrapperResult
{
    [CanBeNull] public AgeRating? Age { get; set; }
    [CanBeNull] public string Author { get; set; }
    [CanBeNull] public string Circle { get; set; }
    [CanBeNull] public string Description { get; set; }
    [CanBeNull] public string Filesize { get; set; }
    [CanBeNull] public List<string> Genres { get; set; }
    [CanBeNull] public string Icon { get; set; }
    [CanBeNull] public List<string> Illustrators { get; set; }
    [CanBeNull] public Dictionary<string, string> Links { get; set; }
    [CanBeNull] public float? Rating { get; set; }
    [CanBeNull] public DateTime? ReleaseDate { get; set; }
    [CanBeNull] public string Series { get; set; }
    [CanBeNull] public List<string> ScenarioWriters { get; set; }
    [CanBeNull] public string MainImage { get; set; }
    [CanBeNull] public List<string> Miscellaneous { get; set; }
    [CanBeNull] public List<string> MusicCreators { get; set; }
    [CanBeNull] public List<string> ProductImages { get; set; }
    [CanBeNull] public string Title { get; set; }
    [CanBeNull] public string UpdateDate { get; set; }
    [CanBeNull] public List<string> VoiceActors { get; set; }
    
    public enum AgeRating
    {
        AllAges,
        RRated,
        Adult
    }
}

public class DLsiteSearchResult
{
    public string Title { get; set; }
    public string Link { get; set; }
    public string Excerpt { get; set; }
}