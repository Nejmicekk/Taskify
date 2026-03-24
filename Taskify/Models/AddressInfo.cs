using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskify.Models;

[Owned]
public class AddressInfo
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    public string? FullAddress { get; set; }
    
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? StreetNumber { get; set; }
    private string? _postCode;
    public string? PostCode
    {
        get => _postCode;
        set
        {
            _postCode = value;
            Region = GetRegionFromPostCode(value);
        }
    }
    public string? Region { get; set; }
    
    private string GetRegionFromPostCode(string? pc)
    {
        if (string.IsNullOrWhiteSpace(pc)) return "Neurčeno";

        string clean = pc.Replace(" ", "");
        if (clean.Length < 3) return "Neurčeno";

        int p3 = int.Parse(clean.Substring(0, 3));
        int p2 = p3 / 10;

        return p3 switch
        {
            var _ when p2 >= 10 && p2 <= 19 => "Hlavní město Praha",
            var _ when p2 >= 25 && p2 <= 29 => "Středočeský kraj",
            var _ when p3 >= 300 && p3 <= 339 => "Plzeňský kraj",
            var _ when p3 >= 350 && p3 <= 369 => "Karlovarský kraj",
            var _ when p3 >= 340 && p3 <= 399 => "Jihočeský kraj",
            var _ when p3 >= 460 && p3 <= 469 => "Liberecký kraj",
            var _ when p2 >= 40 && p2 <= 47 => "Ústecký kraj",
            var _ when p3 >= 510 && p3 <= 519 => "Liberecký kraj",
            var _ when p3 >= 500 && p3 <= 509 => "Královéhradecký kraj",
            var _ when p3 >= 540 && p3 <= 559 => "Královéhradecký kraj",
            var _ when p3 >= 530 && p3 <= 539 => "Pardubický kraj",
            var _ when p3 >= 560 && p3 <= 579 => "Pardubický kraj",
            var _ when p3 >= 580 && p3 <= 599 => "Kraj Vysočina",
            var _ when p2 >= 60 && p2 <= 69 => "Jihomoravský kraj",
            var _ when p3 >= 700 && p3 <= 749 => "Moravskoslezský kraj",
            var _ when p3 >= 750 && p3 <= 769 => "Zlínský kraj",
            var _ when p3 >= 770 && p3 <= 799 => "Olomoucký kraj",
            _ => "Neurčeno"
        };
    }
}