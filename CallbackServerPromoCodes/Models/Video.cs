namespace CallbackServerPromoCodes.Models;

public class Video
{
    public Video(string videoId, string channelId)
    {
        VideoId = videoId;
        ChannelId = channelId;
        Processed = false;
    }
    public int Id { get; set; }

    public string VideoId { get; set; }

    public string ChannelId { get; set; }

    public bool Processed { get; set; }
}