using System;

namespace Kamaq.Finsights.Domain.Entities;

public class StockPrice
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public DateTime Date { get; private set; }
    public decimal Open { get; private set; }
    public decimal High { get; private set; }
    public decimal Low { get; private set; }
    public decimal Close { get; private set; }
    public decimal AdjustedClose { get; private set; }
    public long Volume { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Navigation property
    public Company? Company { get; private set; }
    
    // Required for EF Core
    private StockPrice() { }
    
    public StockPrice(
        Guid companyId,
        string ticker,
        DateTime date,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        decimal adjustedClose,
        long volume)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Ticker = ticker;
        Date = date;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        AdjustedClose = adjustedClose;
        Volume = volume;
        CreatedAt = DateTime.UtcNow;
    }
} 