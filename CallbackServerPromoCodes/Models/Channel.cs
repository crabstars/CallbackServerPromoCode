using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.Models;

[Index(nameof(Id))]
[Index(nameof(Name))]
public class Channel
{
    public Channel()
    {
    }

    public Channel(string channelId, string name)
    {
        Id = channelId;
        Videos = new List<Video>();
        Name = name;
    }

    public string Id { get; set; }

    public string Name { get; set; }
    public List<Video> Videos { get; set; }

    /// <summary>
    ///     in PubSubHub Mode is set to Subscribe or Unsubscribe
    /// </summary>
    public bool Subscribed { get; set; }

    /// <summary>
    ///     if true worker tries to set Subscribed true if its false
    /// </summary>
    [DefaultValue(true)]
    public bool Activated { get; set; } = true;
}