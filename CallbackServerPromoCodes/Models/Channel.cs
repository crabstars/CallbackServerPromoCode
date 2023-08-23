using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.Models;

[Index(nameof(Id))]
[Index(nameof(Name))]
public class Channel
{
    public Channel()
    {
    }

    public Channel(string channelId, bool subscribed)
    {
        Id = channelId;
        Videos = new List<Video>();
        Subscribed = subscribed;
    }

    public string Id { get; set; }

    public string Name { get; set; }
    public List<Video> Videos { get; set; }

    // in PubSubHub Mode is set to Subscribe or Unsubscribe
    public bool Subscribed { get; set; }
}