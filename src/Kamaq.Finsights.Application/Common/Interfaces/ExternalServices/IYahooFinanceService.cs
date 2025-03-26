using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kamaq.Finsights.Domain.Entities;

namespace Kamaq.Finsights.Application.Common.Interfaces.ExternalServices;

public enum TimeInterval
{
    Daily,
    Weekly,
    Monthly
}

public interface IYahooFinanceService
{
    /// <summary>
    /// Gets company profile information by ticker
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company information</returns>
    Task<Company> GetCompanyProfileAsync(string ticker, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets current stock quote for a ticker
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current stock quote information</returns>
    Task<(decimal Price, decimal Change, decimal ChangePercent, decimal Volume, decimal MarketCap)> GetQuoteAsync(
        string ticker, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets historical stock prices for a ticker
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="fromDate">Start date for historical data</param>
    /// <param name="toDate">End date for historical data</param>
    /// <param name="interval">Time interval for data points</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of historical stock prices</returns>
    Task<IEnumerable<StockPrice>> GetHistoricalPricesAsync(
        string ticker,
        DateTime fromDate,
        DateTime toDate,
        TimeInterval interval = TimeInterval.Daily,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets summary financial data for a company
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of financial metrics and their values</returns>
    Task<Dictionary<string, string>> GetFinancialSummaryAsync(
        string ticker,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets analyst recommendations for a ticker
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analyst recommendations and price targets</returns>
    Task<(string Rating, int TotalAnalysts, decimal TargetPrice)> GetAnalystRecommendationsAsync(
        string ticker,
        CancellationToken cancellationToken = default);
} 