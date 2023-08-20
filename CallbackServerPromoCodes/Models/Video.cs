using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.Models;

[Index(nameof(VideoId))]
public class Video
{
    public Video(string videoId, string channelId)
    {
        VideoId = videoId;
        ChannelId = channelId;
        Processed = false;
    }
    
    [Key]
    public string VideoId { get; set; }

    public string ChannelId { get; set; }

    public bool Processed { get; set; }
}