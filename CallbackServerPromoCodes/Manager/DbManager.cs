using CallbackServerPromoCodes.Models;
using CallbackServerPromoCodes.XML.YouTubeFeedSerialization;
using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.Manager;

public static class DbManager
{
    public static async Task<Video> AddVideo(YoutubeFeed feed, AppDbContext context)
    {
        var video = await context.Videos.FirstOrDefaultAsync(v => v.VideoId == feed.Entry.VideoId);
        if (video is not null)
            return video;
        video = new Video(feed.Entry.VideoId, feed.Entry.ChannelId);
        await context.Videos.AddAsync(video);
        await context.SaveChangesAsync();
        return video;
    }
}