using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.Models;

[Index(nameof(Id))]
public class Video
{
    public Video()
    {
    }

    public Video(string videoId, string link, Channel channel)
    {
        Id = videoId;
        Channel = channel;
        Link = link;
        Processed = false;
        Promotions = new List<Promotion>();
    }

    public string Id { get; set; }

    public bool Processed { get; set; }

    public string Link { get; set; }

    public string? Description { get; set; }

    public string? Title { get; set; }

    public List<Promotion> Promotions { get; set; }

    public Channel Channel { get; set; }
}