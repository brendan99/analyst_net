using System;

namespace Kamaq.Finsights.Domain.Entities;

public enum FilingType
{
    Form10K,
    Form10Q,
    Form8K,
    Form4,
    FormDef14A,
    Other
}

public class SecFiling
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string CikNumber { get; private set; } = string.Empty;
    public FilingType FilingType { get; private set; }
    public string FilingTypeDescription { get; private set; } = string.Empty;
    public string AccessionNumber { get; private set; } = string.Empty;
    public DateTime FilingDate { get; private set; }
    public DateTime ReportDate { get; private set; }
    public string FilingUrl { get; private set; } = string.Empty;
    public string? DocumentsUrl { get; private set; }
    public string? HtmlUrl { get; private set; }
    public string? TextUrl { get; private set; }
    public bool IsProcessed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    
    // Navigation property
    public Company? Company { get; private set; }
    
    // Required for EF Core
    private SecFiling() { }
    
    public SecFiling(
        Guid companyId,
        string ticker,
        string companyName,
        string cikNumber,
        FilingType filingType,
        string filingTypeDescription,
        string accessionNumber,
        DateTime filingDate,
        DateTime reportDate,
        string filingUrl,
        string? documentsUrl = null,
        string? htmlUrl = null,
        string? textUrl = null)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        Ticker = ticker;
        CompanyName = companyName;
        CikNumber = cikNumber;
        FilingType = filingType;
        FilingTypeDescription = filingTypeDescription;
        AccessionNumber = accessionNumber;
        FilingDate = filingDate;
        ReportDate = reportDate;
        FilingUrl = filingUrl;
        DocumentsUrl = documentsUrl;
        HtmlUrl = htmlUrl;
        TextUrl = textUrl;
        IsProcessed = false;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
    }
    
    public void UpdateUrls(string? documentsUrl = null, string? htmlUrl = null, string? textUrl = null)
    {
        DocumentsUrl = documentsUrl ?? DocumentsUrl;
        HtmlUrl = htmlUrl ?? HtmlUrl;
        TextUrl = textUrl ?? TextUrl;
    }
} 