using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kamaq.Finsights.Infrastructure.External.Models.YahooFinance;

#region Quote Response

public class YahooQuoteResponse
{
    [JsonProperty("quoteResponse")]
    public QuoteResponseData QuoteResponse { get; set; } = new();
}

public class QuoteResponseData
{
    [JsonProperty("result")]
    public List<QuoteResult> Result { get; set; } = new();
    
    [JsonProperty("error")]
    public string? Error { get; set; }
}

public class QuoteResult
{
    [JsonProperty("regularMarketPrice")]
    public decimal? RegularMarketPrice { get; set; }
    
    [JsonProperty("regularMarketChange")]
    public decimal? RegularMarketChange { get; set; }
    
    [JsonProperty("regularMarketChangePercent")]
    public decimal? RegularMarketChangePercent { get; set; }
    
    [JsonProperty("regularMarketVolume")]
    public long? RegularMarketVolume { get; set; }
    
    [JsonProperty("marketCap")]
    public decimal? MarketCap { get; set; }
    
    [JsonProperty("shortName")]
    public string? ShortName { get; set; }
    
    [JsonProperty("longName")]
    public string? LongName { get; set; }
    
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonProperty("exchange")]
    public string? Exchange { get; set; }
    
    [JsonProperty("quoteType")]
    public string? QuoteType { get; set; }
}

#endregion

#region Chart Response

public class YahooChartResponse
{
    [JsonProperty("chart")]
    public ChartData Chart { get; set; } = new();
}

public class ChartData
{
    [JsonProperty("result")]
    public List<ChartResult> Result { get; set; } = new();
    
    [JsonProperty("error")]
    public string? Error { get; set; }
}

public class ChartResult
{
    [JsonProperty("meta")]
    public ChartMeta Meta { get; set; } = new();
    
    [JsonProperty("timestamp")]
    public List<long>? Timestamp { get; set; }
    
    [JsonProperty("indicators")]
    public Indicators Indicators { get; set; } = new();
}

public class ChartMeta
{
    [JsonProperty("currency")]
    public string? Currency { get; set; }
    
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonProperty("exchangeName")]
    public string? ExchangeName { get; set; }
    
    [JsonProperty("instrumentType")]
    public string? InstrumentType { get; set; }
    
    [JsonProperty("regularMarketPrice")]
    public decimal? RegularMarketPrice { get; set; }
}

public class Indicators
{
    [JsonProperty("quote")]
    public List<Quote> Quote { get; set; } = new();
    
    [JsonProperty("adjclose")]
    public List<AdjClose>? AdjClose { get; set; }
}

public class Quote
{
    [JsonProperty("open")]
    public List<decimal?> Open { get; set; } = new();
    
    [JsonProperty("high")]
    public List<decimal?> High { get; set; } = new();
    
    [JsonProperty("low")]
    public List<decimal?> Low { get; set; } = new();
    
    [JsonProperty("close")]
    public List<decimal?> Close { get; set; } = new();
    
    [JsonProperty("volume")]
    public List<long?> Volume { get; set; } = new();
}

public class AdjClose
{
    [JsonProperty("adjclose")]
    public List<decimal?> Values { get; set; } = new();
}

#endregion

#region Company Profile

public class YahooAssetProfile
{
    [JsonProperty("assetProfile")]
    public AssetProfile? AssetProfile { get; set; }
}

public class AssetProfile
{
    [JsonProperty("address1")]
    public string? Address1 { get; set; }
    
    [JsonProperty("city")]
    public string? City { get; set; }
    
    [JsonProperty("state")]
    public string? State { get; set; }
    
    [JsonProperty("zip")]
    public string? Zip { get; set; }
    
    [JsonProperty("country")]
    public string? Country { get; set; }
    
    [JsonProperty("phone")]
    public string? Phone { get; set; }
    
    [JsonProperty("website")]
    public string? Website { get; set; }
    
    [JsonProperty("industry")]
    public string? Industry { get; set; }
    
    [JsonProperty("sector")]
    public string? Sector { get; set; }
    
    [JsonProperty("exchange")]
    public string? Exchange { get; set; }
    
    [JsonProperty("longBusinessSummary")]
    public string? LongBusinessSummary { get; set; }
    
    [JsonProperty("fullTimeEmployees")]
    public int? FullTimeEmployees { get; set; }
    
    [JsonProperty("companyOfficers")]
    public List<CompanyOfficer> CompanyOfficers { get; set; } = new();
}

public class CompanyOfficer
{
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("title")]
    public string? Title { get; set; }
    
    [JsonProperty("yearBorn")]
    public int? YearBorn { get; set; }
}

#endregion

#region Financial Data

public class YahooFinancialData
{
    [JsonProperty("financialData")]
    public FinancialData? FinancialData { get; set; }
}

public class FinancialData
{
    [JsonProperty("currentPrice")]
    public ValueObject? CurrentPrice { get; set; }
    
    [JsonProperty("targetHighPrice")]
    public ValueObject? TargetHighPrice { get; set; }
    
    [JsonProperty("targetLowPrice")]
    public ValueObject? TargetLowPrice { get; set; }
    
    [JsonProperty("targetMeanPrice")]
    public ValueObject? TargetMeanPrice { get; set; }
    
    [JsonProperty("recommendationMean")]
    public ValueObject? RecommendationMean { get; set; }
    
    [JsonProperty("recommendationKey")]
    public string? RecommendationKey { get; set; }
    
    [JsonProperty("numberOfAnalystOpinions")]
    public ValueObject? NumberOfAnalystOpinions { get; set; }
    
    [JsonProperty("totalCash")]
    public ValueObject? TotalCash { get; set; }
    
    [JsonProperty("totalDebt")]
    public ValueObject? TotalDebt { get; set; }
    
    [JsonProperty("totalRevenue")]
    public ValueObject? TotalRevenue { get; set; }
    
    [JsonProperty("ebitda")]
    public ValueObject? EBITDA { get; set; }
    
    [JsonProperty("grossMargins")]
    public ValueObject? GrossMargins { get; set; }
    
    [JsonProperty("operatingMargins")]
    public ValueObject? OperatingMargins { get; set; }
    
    [JsonProperty("profitMargins")]
    public ValueObject? ProfitMargins { get; set; }
    
    [JsonProperty("freeCashflow")]
    public ValueObject? FreeCashflow { get; set; }
    
    [JsonProperty("returnOnAssets")]
    public ValueObject? ReturnOnAssets { get; set; }
    
    [JsonProperty("returnOnEquity")]
    public ValueObject? ReturnOnEquity { get; set; }
    
    [JsonProperty("earningsGrowth")]
    public ValueObject? EarningsGrowth { get; set; }
    
    [JsonProperty("revenueGrowth")]
    public ValueObject? RevenueGrowth { get; set; }
}

public class ValueObject
{
    [JsonProperty("raw")]
    public decimal Raw { get; set; }
    
    [JsonProperty("fmt")]
    public string? Formatted { get; set; }
}

#endregion 