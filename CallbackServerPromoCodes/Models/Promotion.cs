using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.Models;

[Index(nameof(Id))]
[Index(nameof(Company))]
public class Promotion
{
    public Promotion()
    {
    }

    public Promotion(string? code, string? link, string company, Video video)
    {
        Code = code;
        Link = link;
        Video = video;
        Company = company;
    }

    public int Id { get; set; }

    public string? Code { get; set; }

    public string? Link { get; set; }

    public string Company { get; set; }

    public Video Video { get; set; }
}