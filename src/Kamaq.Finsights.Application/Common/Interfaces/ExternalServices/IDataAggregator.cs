using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kamaq.Finsights.Domain.Entities;

namespace Kamaq.Finsights.Application.Common.Interfaces.ExternalServices;

/// <summary>
/// Aggregates data from multiple sources and provides a unified interface
/// </summary>
public interface IDataAggregator
{
    /// <summary>
    /// Gets comprehensive company information by ticker
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete company profile</returns>
    Task<Company> GetCompanyProfileAsync(
        string ticker, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets stock price history for a ticker
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="interval">Time interval</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of historical stock prices</returns>
    Task<IEnumerable<StockPrice>> GetHistoricalPricesAsync(
        string ticker,
        DateTime fromDate,
        DateTime toDate,
        TimeInterval interval = TimeInterval.Daily,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets SEC filings for a company
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="filingTypes">Types of filings to retrieve</param>
    /// <param name="limit">Maximum number of filings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of SEC filings</returns>
    Task<IEnumerable<SecFiling>> GetSecFilingsAsync(
        string ticker,
        IEnumerable<FilingType>? filingTypes = null,
        int limit = 20,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets financial statements from a filing
    /// </summary>
    /// <param name="filing">SEC filing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of financial statements</returns>
    Task<IEnumerable<FinancialStatement>> GetFinancialStatementsAsync(
        SecFiling filing,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets financial metrics and ratios for a company
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of financial metrics and ratios</returns>
    Task<Dictionary<string, string>> GetFinancialMetricsAsync(
        string ticker,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets analyst recommendations for a company
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analyst recommendations</returns>
    Task<(string Rating, int TotalAnalysts, decimal TargetPrice)> GetAnalystRecommendationsAsync(
        string ticker,
        CancellationToken cancellationToken = default);
} 