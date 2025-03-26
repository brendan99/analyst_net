using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kamaq.Finsights.Application.Common.Interfaces;
using Kamaq.Finsights.Application.Common.Interfaces.ExternalServices;
using Kamaq.Finsights.Domain.Entities;
using Kamaq.Finsights.Infrastructure.External.Models.YahooFinance;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kamaq.Finsights.Infrastructure.External.Services;

public class YahooFinanceService : IYahooFinanceService
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService _cacheService;
    private readonly ILogger<YahooFinanceService> _logger;
    
    // Base Yahoo Finance API URLs
    private const string BaseUrl = "https://query1.finance.yahoo.com/v8/finance";
    private const string QuoteUrl = BaseUrl + "/quote";
    private const string ChartUrl = BaseUrl + "/chart";
    private const string ModulesUrl = BaseUrl + "/quoteSummary";
    
    public YahooFinanceService(
        HttpClient httpClient,
        ICacheService cacheService,
        ILogger<YahooFinanceService> logger)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets company profile information by ticker
    /// </summary>
    public async Task<Company> GetCompanyProfileAsync(string ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"company_profile_{ticker}";
            var cachedCompany = await _cacheService.GetAsync<Company>(cacheKey);
            if (cachedCompany != null)
            {
                _logger.LogInformation("Retrieved company profile for {Ticker} from cache", ticker);
                return cachedCompany;
            }
            
            // Make calls to get company info from different modules
            var quoteTask = GetQuoteAsync(ticker, cancellationToken);
            var profileTask = FetchModuleDataAsync<YahooAssetProfile>(ticker, "assetProfile", cancellationToken);
            
            await Task.WhenAll(quoteTask, profileTask);
            
            var quote = await quoteTask;
            var profile = await profileTask;
            
            if (profile?.AssetProfile == null)
            {
                throw new InvalidOperationException($"Could not retrieve profile data for {ticker}");
            }
            
            // Create and cache the company
            var company = new Company(
                ticker,
                profile.AssetProfile.LongBusinessSummary != null ? profile.AssetProfile.LongBusinessSummary.Split('.')[0] : ticker,
                profile.AssetProfile.Exchange ?? "Unknown",
                profile.AssetProfile.Sector ?? "Unknown",
                profile.AssetProfile.Industry ?? "Unknown");
            
            company.Update(
                name: profile.AssetProfile.LongBusinessSummary != null ? profile.AssetProfile.LongBusinessSummary.Split('.')[0] : ticker,
                exchange: profile.AssetProfile.Exchange ?? "Unknown",
                sector: profile.AssetProfile.Sector ?? "Unknown",
                industry: profile.AssetProfile.Industry ?? "Unknown",
                website: profile.AssetProfile.Website,
                description: profile.AssetProfile.LongBusinessSummary,
                marketCap: quote.MarketCap
            );
            
            await _cacheService.SetAsync(cacheKey, company, TimeSpan.FromHours(12));
            _logger.LogInformation("Retrieved and cached company profile for {Ticker}", ticker);
            
            return company;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company profile for {Ticker}", ticker);
            throw;
        }
    }
    
    /// <summary>
    /// Gets current stock quote for a ticker
    /// </summary>
    public async Task<(decimal Price, decimal Change, decimal ChangePercent, decimal Volume, decimal MarketCap)> GetQuoteAsync(
        string ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"quote_{ticker}";
            var cachedQuote = await _cacheService.GetAsync<(decimal, decimal, decimal, decimal, decimal)>(cacheKey);
            if (cachedQuote.Item1 != 0)
            {
                _logger.LogInformation("Retrieved quote for {Ticker} from cache", ticker);
                return cachedQuote;
            }
            
            // Construct the URL
            var url = $"{QuoteUrl}?symbols={ticker}";
            
            // Make the request
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var quoteResponse = JsonConvert.DeserializeObject<YahooQuoteResponse>(response);
            
            if (quoteResponse?.QuoteResponse?.Result == null || !quoteResponse.QuoteResponse.Result.Any())
            {
                throw new InvalidOperationException($"Could not retrieve quote data for {ticker}");
            }
            
            var quoteResult = quoteResponse.QuoteResponse.Result.First();
            
            var quote = (
                Price: quoteResult.RegularMarketPrice ?? 0,
                Change: quoteResult.RegularMarketChange ?? 0,
                ChangePercent: quoteResult.RegularMarketChangePercent ?? 0,
                Volume: quoteResult.RegularMarketVolume ?? 0,
                MarketCap: quoteResult.MarketCap ?? 0
            );
            
            // Cache the result for 15 minutes
            await _cacheService.SetAsync(cacheKey, quote, TimeSpan.FromMinutes(15));
            _logger.LogInformation("Retrieved and cached quote for {Ticker}", ticker);
            
            return quote;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quote for {Ticker}", ticker);
            throw;
        }
    }
    
    /// <summary>
    /// Gets historical stock prices for a ticker
    /// </summary>
    public async Task<IEnumerable<StockPrice>> GetHistoricalPricesAsync(
        string ticker,
        DateTime fromDate,
        DateTime toDate,
        TimeInterval interval = TimeInterval.Daily,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert interval to Yahoo format
            var yahooInterval = interval switch
            {
                TimeInterval.Daily => "1d",
                TimeInterval.Weekly => "1wk",
                TimeInterval.Monthly => "1mo",
                _ => "1d"
            };
            
            // Check cache first
            var cacheKey = $"history_{ticker}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}_{yahooInterval}";
            var cachedPrices = await _cacheService.GetAsync<List<StockPrice>>(cacheKey);
            if (cachedPrices != null && cachedPrices.Any())
            {
                _logger.LogInformation("Retrieved historical prices for {Ticker} from cache", ticker);
                return cachedPrices;
            }
            
            // Convert dates to Unix timestamps
            var fromUnix = new DateTimeOffset(fromDate).ToUnixTimeSeconds();
            var toUnix = new DateTimeOffset(toDate).ToUnixTimeSeconds();
            
            // Construct the URL
            var url = $"{ChartUrl}/{ticker}?period1={fromUnix}&period2={toUnix}&interval={yahooInterval}&includeAdjustedClose=true";
            
            // Make the request
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var chartResponse = JsonConvert.DeserializeObject<YahooChartResponse>(response);
            
            if (chartResponse?.Chart?.Result == null || !chartResponse.Chart.Result.Any())
            {
                throw new InvalidOperationException($"Could not retrieve historical data for {ticker}");
            }
            
            var chartResult = chartResponse.Chart.Result.First();
            
            if (chartResult.Timestamp == null || chartResult.Indicators?.Quote == null || !chartResult.Indicators.Quote.Any())
            {
                throw new InvalidOperationException($"Historical data for {ticker} is incomplete");
            }
            
            var quote = chartResult.Indicators.Quote.First();
            var adjustedClose = chartResult.Indicators.AdjClose?.FirstOrDefault();
            
            // Get company info for the ID
            var companyTask = GetCompanyProfileAsync(ticker, cancellationToken);
            var company = await companyTask;
            
            var prices = new List<StockPrice>();
            
            for (int i = 0; i < chartResult.Timestamp.Count; i++)
            {
                if (i >= quote.Open.Count || i >= quote.High.Count || i >= quote.Low.Count || 
                    i >= quote.Close.Count || i >= quote.Volume.Count)
                {
                    continue;
                }
                
                if (quote.Open[i] == null || quote.High[i] == null || quote.Low[i] == null || 
                    quote.Close[i] == null || quote.Volume[i] == null)
                {
                    continue;
                }
                
                var timestamp = DateTimeOffset.FromUnixTimeSeconds(chartResult.Timestamp[i]).DateTime;
                var adjClose = adjustedClose != null && i < adjustedClose.Values.Count 
                    ? adjustedClose.Values[i] ?? quote.Close[i]!.Value 
                    : quote.Close[i]!.Value;
                
                prices.Add(new StockPrice(
                    company.Id,
                    ticker,
                    timestamp,
                    quote.Open[i]!.Value,
                    quote.High[i]!.Value,
                    quote.Low[i]!.Value,
                    quote.Close[i]!.Value,
                    adjClose,
                    quote.Volume[i]!.Value
                ));
            }
            
            // Cache the result for 24 hours if historical data
            await _cacheService.SetAsync(cacheKey, prices, TimeSpan.FromHours(24));
            _logger.LogInformation("Retrieved {Count} historical prices for {Ticker}", prices.Count, ticker);
            
            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical prices for {Ticker}", ticker);
            throw;
        }
    }
    
    /// <summary>
    /// Gets summary financial data for a company
    /// </summary>
    public async Task<Dictionary<string, string>> GetFinancialSummaryAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"financial_summary_{ticker}";
            var cachedSummary = await _cacheService.GetAsync<Dictionary<string, string>>(cacheKey);
            if (cachedSummary != null && cachedSummary.Any())
            {
                _logger.LogInformation("Retrieved financial summary for {Ticker} from cache", ticker);
                return cachedSummary;
            }
            
            // Fetch financial data
            var financialData = await FetchModuleDataAsync<YahooFinancialData>(ticker, "financialData", cancellationToken);
            
            if (financialData?.FinancialData == null)
            {
                throw new InvalidOperationException($"Could not retrieve financial data for {ticker}");
            }
            
            var data = financialData.FinancialData;
            
            // Extract the relevant metrics
            var summary = new Dictionary<string, string>
            {
                ["Current Price"] = data.CurrentPrice?.Formatted ?? "N/A",
                ["ROE"] = data.ReturnOnEquity?.Formatted ?? "N/A",
                ["ROA"] = data.ReturnOnAssets?.Formatted ?? "N/A",
                ["Gross Margin"] = data.GrossMargins?.Formatted ?? "N/A",
                ["Operating Margin"] = data.OperatingMargins?.Formatted ?? "N/A",
                ["Profit Margin"] = data.ProfitMargins?.Formatted ?? "N/A",
                ["Total Cash"] = data.TotalCash?.Formatted ?? "N/A",
                ["Total Debt"] = data.TotalDebt?.Formatted ?? "N/A",
                ["Revenue"] = data.TotalRevenue?.Formatted ?? "N/A",
                ["EBITDA"] = data.EBITDA?.Formatted ?? "N/A",
                ["Free Cash Flow"] = data.FreeCashflow?.Formatted ?? "N/A",
                ["Earnings Growth"] = data.EarningsGrowth?.Formatted ?? "N/A",
                ["Revenue Growth"] = data.RevenueGrowth?.Formatted ?? "N/A"
            };
            
            // Cache the result for 24 hours
            await _cacheService.SetAsync(cacheKey, summary, TimeSpan.FromHours(24));
            _logger.LogInformation("Retrieved and cached financial summary for {Ticker}", ticker);
            
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial summary for {Ticker}", ticker);
            throw;
        }
    }
    
    /// <summary>
    /// Gets analyst recommendations for a ticker
    /// </summary>
    public async Task<(string Rating, int TotalAnalysts, decimal TargetPrice)> GetAnalystRecommendationsAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"analyst_recommendations_{ticker}";
            var cachedRecommendations = await _cacheService.GetAsync<(string, int, decimal)>(cacheKey);
            if (!string.IsNullOrEmpty(cachedRecommendations.Item1))
            {
                _logger.LogInformation("Retrieved analyst recommendations for {Ticker} from cache", ticker);
                return cachedRecommendations;
            }
            
            // Fetch financial data
            var financialData = await FetchModuleDataAsync<YahooFinancialData>(ticker, "financialData", cancellationToken);
            
            if (financialData?.FinancialData == null)
            {
                throw new InvalidOperationException($"Could not retrieve financial data for {ticker}");
            }
            
            var data = financialData.FinancialData;
            
            var rating = data.RecommendationKey ?? "N/A";
            var totalAnalysts = data.NumberOfAnalystOpinions?.Raw ?? 0;
            var targetPrice = data.TargetMeanPrice?.Raw ?? 0;
            
            var recommendations = (rating, (int)totalAnalysts, targetPrice);
            
            // Cache the result for 24 hours
            await _cacheService.SetAsync(cacheKey, recommendations, TimeSpan.FromHours(24));
            _logger.LogInformation("Retrieved and cached analyst recommendations for {Ticker}", ticker);
            
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyst recommendations for {Ticker}", ticker);
            throw;
        }
    }
    
    #region Private Methods
    
    /// <summary>
    /// Fetches data from a specific module
    /// </summary>
    private async Task<T?> FetchModuleDataAsync<T>(
        string ticker,
        string module,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            var url = $"{ModulesUrl}/{ticker}?modules={module}";
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var responseObj = JsonConvert.DeserializeObject<T>(response);
            return responseObj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching module data {Module} for {Ticker}", module, ticker);
            throw;
        }
    }
    
    #endregion
} 