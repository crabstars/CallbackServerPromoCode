using CallbackServerPromoCodes.Models;
using CallbackServerPromoCodes.XML.YouTubeFeedSerialization;
using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.Manager;

public static class DbManager
{
    public static async Task<Video> AddVideo(YoutubeFeed feed, Channel channel, AppDbContext context)
    {
        var video = await context.Videos.FirstOrDefaultAsync(v => v.Id == feed.Entry.VideoId);
        if (video is not null)
            return video;
        video = new Video(feed.Entry.VideoId, feed.Entry.Link.Href, channel);
        await context.Videos.AddAsync(video);
        await context.SaveChangesAsync();
        return video;
    }
}