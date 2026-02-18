using Microsoft.EntityFrameworkCore;

namespace Taskify.Models;

[Owned]
public class AddressInfo
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? FullAddress { get; set; }
    public string? City { get; set; }
}