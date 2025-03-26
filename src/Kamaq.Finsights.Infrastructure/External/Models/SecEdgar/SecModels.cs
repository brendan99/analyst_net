using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kamaq.Finsights.Infrastructure.External.Models.SecEdgar;

#region Company Search

public class CompanySearchResponse
{
    [JsonProperty("filings")]
    public FilingsData? Filings { get; set; }
}

public class FilingsData
{
    [JsonProperty("recent")]
    public RecentFilings? Recent { get; set; }
    
    [JsonProperty("files")]
    public List<FilingFile>? Files { get; set; }
}

public class RecentFilings
{
    [JsonProperty("accessionNumber")]
    public List<string>? AccessionNumber { get; set; }
    
    [JsonProperty("filingDate")]
    public List<string>? FilingDate { get; set; }
    
    [JsonProperty("reportDate")]
    public List<string>? ReportDate { get; set; }
    
    [JsonProperty("form")]
    public List<string>? Form { get; set; }
    
    [JsonProperty("primaryDocument")]
    public List<string>? PrimaryDocument { get; set; }
}

public class FilingFile
{
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("filingCount")]
    public int? FilingCount { get; set; }
    
    [JsonProperty("filingFrom")]
    public string? FilingFrom { get; set; }
    
    [JsonProperty("filingTo")]
    public string? FilingTo { get; set; }
}

#endregion

#region Company Info

public class CompanyInfoResponse
{
    [JsonProperty("cik")]
    public string? Cik { get; set; }
    
    [JsonProperty("entityType")]
    public string? EntityType { get; set; }
    
    [JsonProperty("sic")]
    public string? Sic { get; set; }
    
    [JsonProperty("sicDescription")]
    public string? SicDescription { get; set; }
    
    [JsonProperty("insiderTransactionForOwnerExists")]
    public bool? InsiderTransactionForOwnerExists { get; set; }
    
    [JsonProperty("insiderTransactionForIssuerExists")]
    public bool? InsiderTransactionForIssuerExists { get; set; }
    
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("tickers")]
    public List<string>? Tickers { get; set; }
    
    [JsonProperty("exchanges")]
    public List<string>? Exchanges { get; set; }
    
    [JsonProperty("ein")]
    public string? Ein { get; set; }
    
    [JsonProperty("description")]
    public string? Description { get; set; }
    
    [JsonProperty("website")]
    public string? Website { get; set; }
    
    [JsonProperty("address")]
    public string? Address { get; set; }
    
    [JsonProperty("phone")]
    public string? Phone { get; set; }
    
    [JsonProperty("flags")]
    public string? Flags { get; set; }
    
    [JsonProperty("formerNames")]
    public List<FormerName>? FormerNames { get; set; }
    
    [JsonProperty("filings")]
    public FilingsData? Filings { get; set; }
}

public class FormerName
{
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("from")]
    public string? From { get; set; }
    
    [JsonProperty("to")]
    public string? To { get; set; }
}

#endregion

#region CIK Lookup

public class CikLookupResponse : Dictionary<string, CikData>
{
}

public class CikData
{
    [JsonProperty("cik_str")]
    public string? CikStr { get; set; }
    
    [JsonProperty("ticker")]
    public string? Ticker { get; set; }
    
    [JsonProperty("title")]
    public string? Title { get; set; }
}

#endregion

#region Filing Details

public class FilingDetailsResponse
{
    [JsonProperty("cik")]
    public string? Cik { get; set; }
    
    [JsonProperty("ticker")]
    public string? Ticker { get; set; }
    
    [JsonProperty("companyName")]
    public string? CompanyName { get; set; }
    
    [JsonProperty("formType")]
    public string? FormType { get; set; }
    
    [JsonProperty("filingDate")]
    public string? FilingDate { get; set; }
    
    [JsonProperty("reportDate")]
    public string? ReportDate { get; set; }
    
    [JsonProperty("accessionNumber")]
    public string? AccessionNumber { get; set; }
    
    [JsonProperty("fileNumber")]
    public string? FileNumber { get; set; }
    
    [JsonProperty("items")]
    public List<string>? Items { get; set; }
    
    [JsonProperty("documents")]
    public List<Document>? Documents { get; set; }
}

public class Document
{
    [JsonProperty("documentUrl")]
    public string? DocumentUrl { get; set; }
    
    [JsonProperty("type")]
    public string? Type { get; set; }
    
    [JsonProperty("description")]
    public string? Description { get; set; }
    
    [JsonProperty("filename")]
    public string? Filename { get; set; }
    
    [JsonProperty("size")]
    public long? Size { get; set; }
}

#endregion 