using System;

namespace Kamaq.Finsights.Domain.Entities;

public class Company
{
    public Guid Id { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Exchange { get; private set; } = string.Empty;
    public string Sector { get; private set; } = string.Empty;
    public string Industry { get; private set; } = string.Empty;
    public string? Website { get; private set; }
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? CikNumber { get; private set; }
    public decimal? MarketCap { get; private set; }
    public DateTime LastUpdated { get; private set; }

    // Required for EF Core
    private Company() { }
    
    public Company(
        string ticker, 
        string name, 
        string exchange, 
        string sector, 
        string industry)
    {
        Id = Guid.NewGuid();
        Ticker = ticker;
        Name = name;
        Exchange = exchange;
        Sector = sector;
        Industry = industry;
        LastUpdated = DateTime.UtcNow;
    }
    
    public void Update(
        string name,
        string exchange,
        string sector,
        string industry,
        string? website = null,
        string? description = null,
        string? logoUrl = null,
        string? cikNumber = null,
        decimal? marketCap = null)
    {
        Name = name;
        Exchange = exchange;
        Sector = sector;
        Industry = industry;
        Website = website ?? Website;
        Description = description ?? Description;
        LogoUrl = logoUrl ?? LogoUrl;
        CikNumber = cikNumber ?? CikNumber;
        MarketCap = marketCap ?? MarketCap;
        LastUpdated = DateTime.UtcNow;
    }
} 