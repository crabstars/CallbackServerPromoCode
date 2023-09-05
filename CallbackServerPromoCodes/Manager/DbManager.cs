using CallbackServerPromoCodes.DbModels;
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
        video = new Video(feed.Entry.VideoId, feed.Entry.Link.Href, feed.Title, channel);
        await context.Videos.AddAsync(video);
        await context.SaveChangesAsync();
        return video;
    }

    public static async Task<Channel?> AddChannel(AppDbContext context, Channel channel,
        CancellationToken cancellationToken)
    {
        if (context.Channels.Any(c => c.Id == channel.Id))
            return null;

        await context.Channels.AddAsync(channel, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return channel;
    }

    public static async Task<Channel?> DeleteChannel(AppDbContext context, string channelId,
        CancellationToken cancellationToken)
    {
        var channel = await context.Channels.FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);
        if (channel is null)
            return null;
        context.Channels.Remove(channel);
        await context.SaveChangesAsync(cancellationToken);
        return channel;
    }
}