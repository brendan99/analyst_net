using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kamaq.Finsights.Application.Common.Interfaces;
using Kamaq.Finsights.Application.Common.Interfaces.ExternalServices;
using Kamaq.Finsights.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Kamaq.Finsights.Infrastructure.External.Services;

public class DataAggregator : IDataAggregator
{
    private readonly IYahooFinanceService _yahooFinanceService;
    private readonly ISecEdgarService _secEdgarService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DataAggregator> _logger;
    
    public DataAggregator(
        IYahooFinanceService yahooFinanceService,
        ISecEdgarService secEdgarService,
        ICacheService cacheService,
        ILogger<DataAggregator> logger)
    {
        _yahooFinanceService = yahooFinanceService;
        _secEdgarService = secEdgarService;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets comprehensive company information by ticker
    /// </summary>
    public async Task<Company> GetCompanyProfileAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving comprehensive company profile for {Ticker}", ticker);
            
            // Get profile from Yahoo Finance
            var yahooProfileTask = _yahooFinanceService.GetCompanyProfileAsync(ticker, cancellationToken);
            
            // Get CIK number from SEC
            var secInfoTask = _secEdgarService.GetCompanyInfoAsync(ticker, cancellationToken);
            
            await Task.WhenAll(yahooProfileTask, secInfoTask);
            
            var company = await yahooProfileTask;
            var (cikNumber, companyName) = await secInfoTask;
            
            // Update the company with CIK number
            company.Update(
                company.Name,
                company.Exchange,
                company.Sector,
                company.Industry,
                company.Website,
                company.Description,
                company.LogoUrl,
                cikNumber,
                company.MarketCap);
            
            return company;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comprehensive company profile for {Ticker}", ticker);
            throw;
        }
    }
    
    /// <summary>
    /// Gets stock price history for a ticker
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
            _logger.LogInformation("Retrieving historical prices for {Ticker} from {FromDate} to {ToDate}", 
                ticker, fromDate, toDate);
            
            // Delegate to Yahoo Finance service
            return await _yahooFinanceService.GetHistoricalPricesAsync(
                ticker, fromDate, toDate, interval, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical prices for {Ticker}", ticker);
            throw;
        }
    }
    
    /// <summary>
    /// Gets SEC filings for a company
    /// </summary>
    public async Task<IEnumerable<SecFiling>> GetSecFilingsAsync(
        string ticker,
        IEnumerable<FilingType>? filingTypes = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving SEC filings for {Ticker}", ticker);
            
            // Get CIK number first
            var (cikNumber, _) = await _secEdgarService.GetCompanyInfoAsync(ticker, cancellationToken);
            
            // Then get filings
            return await _secEdgarService.GetCompanyFilingsAsync(
                cikNumber, filingTypes, limit, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving SEC filings for {Ticker}", ticker);
            throw;
        }
    }
    
    /// <summary>
    /// Gets financial statements from a filing
    /// </summary>
    public async Task<IEnumerable<FinancialStatement>> GetFinancialStatementsAsync(
        SecFiling filing,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving financial statements from filing {AccessionNumber}", 
                filing.AccessionNumber);
            
            // Get detailed filing info if needed
            if (string.IsNullOrEmpty(filing.HtmlUrl) && string.IsNullOrEmpty(filing.TextUrl))
            {
                filing = await _secEdgarService.GetFilingDetailsAsync(
                    filing.AccessionNumber, filing.CikNumber, cancellationToken);
            }
            
            // Download and parse the filing
            var contentUrl = filing.HtmlUrl ?? filing.TextUrl ?? filing.FilingUrl;
            var content = await _secEdgarService.DownloadFilingContentAsync(contentUrl, cancellationToken);
            
            // Extract financial statements
            return await _secEdgarService.ExtractFinancialDataAsync(
                content, filing.FilingType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial statements from filing {AccessionNumber}",
                filing.AccessionNumber);
            throw;
        }
    }
    
    /// <summary>
    /// Gets financial metrics and ratios for a company
    /// </summary>
    public async Task<Dictionary<string, string>> GetFinancialMetricsAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving financial metrics for {Ticker}", ticker);
            
            // Check cache first
            var cacheKey = $"financial_metrics_{ticker}";
            var cachedMetrics = await _cacheService.GetAsync<Dictionary<string, string>>(cacheKey);
            if (cachedMetrics != null && cachedMetrics.Any())
            {
                return cachedMetrics;
            }
            
            // Get summary data from Yahoo Finance
            var yahooSummary = await _yahooFinanceService.GetFinancialSummaryAsync(ticker, cancellationToken);
            
            // In a more complete implementation, we would combine this with data from SEC filings
            // For now, just return the Yahoo data
            
            // Cache for 24 hours
            await _cacheService.SetAsync(cacheKey, yahooSummary, TimeSpan.FromHours(24));
            
            return yahooSummary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving financial metrics for {Ticker}", ticker);
            throw;
        }
    }
    
    /// <summary>
    /// Gets analyst recommendations for a company
    /// </summary>
    public async Task<(string Rating, int TotalAnalysts, decimal TargetPrice)> GetAnalystRecommendationsAsync(
        string ticker,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving analyst recommendations for {Ticker}", ticker);
            
            // Delegate to Yahoo Finance service
            return await _yahooFinanceService.GetAnalystRecommendationsAsync(ticker, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analyst recommendations for {Ticker}", ticker);
            throw;
        }
    }
} 