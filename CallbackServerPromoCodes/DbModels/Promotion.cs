using Microsoft.EntityFrameworkCore;

namespace CallbackServerPromoCodes.DbModels;

[Index(nameof(Id))]
[Index(nameof(Product))]
public class Promotion
{
    public Promotion()
    {
    }

    public Promotion(string? code, string? link, string product)
    {
        Code = code;
        Link = link;
        Product = product;
        Added = DateTime.Now;
    }

    public int Id { get; set; }

    public string? Code { get; set; }

    public string? Link { get; set; }

    public string Product { get; set; }

    public Video Video { get; set; }

    public DateTime Added { get; set; }
}