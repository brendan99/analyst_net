using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kamaq.Finsights.Domain.Entities;

namespace Kamaq.Finsights.Application.Common.Interfaces.ExternalServices;

public interface ISecEdgarService
{
    /// <summary>
    /// Gets company information by ticker
    /// </summary>
    /// <param name="ticker">Company ticker symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Basic company information including CIK number</returns>
    Task<(string CikNumber, string CompanyName)> GetCompanyInfoAsync(string ticker, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the latest filings for a company by CIK
    /// </summary>
    /// <param name="cikNumber">Company CIK number (without leading zeros)</param>
    /// <param name="filingTypes">Optional specific filing types to filter by</param>
    /// <param name="limit">Maximum number of filings to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of filing metadata</returns>
    Task<IEnumerable<SecFiling>> GetCompanyFilingsAsync(
        string cikNumber,
        IEnumerable<FilingType>? filingTypes = null,
        int limit = 20,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets detailed information about a specific filing
    /// </summary>
    /// <param name="accessionNumber">Filing accession number</param>
    /// <param name="cikNumber">Company CIK number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed filing information including URLs to documents</returns>
    Task<SecFiling> GetFilingDetailsAsync(
        string accessionNumber,
        string cikNumber,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Downloads the content of a filing
    /// </summary>
    /// <param name="url">URL to the filing document</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filing content as string</returns>
    Task<string> DownloadFilingContentAsync(
        string url,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extracts financial data from a filing document
    /// </summary>
    /// <param name="filingContent">Filing content</param>
    /// <param name="filingType">Type of filing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of financial statements extracted from the filing</returns>
    Task<IEnumerable<FinancialStatement>> ExtractFinancialDataAsync(
        string filingContent,
        FilingType filingType,
        CancellationToken cancellationToken = default);
} 