using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Kamaq.Finsights.Application.Common.Interfaces;
using Kamaq.Finsights.Application.Common.Interfaces.ExternalServices;
using Kamaq.Finsights.Domain.Entities;
using Kamaq.Finsights.Infrastructure.External.Models.SecEdgar;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kamaq.Finsights.Infrastructure.External.Services;

public class SecEdgarService : ISecEdgarService
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SecEdgarService> _logger;
    
    // Base SEC EDGAR API URLs
    private const string BaseUrl = "https://data.sec.gov";
    private const string CompanyUrl = BaseUrl + "/submissions/CIK{0}.json";
    private const string FilingsUrl = BaseUrl + "/api/xbrl/companyfacts/CIK{0}.json";
    private const string CikLookupUrl = BaseUrl + "/files/company_tickers.json";
    
    // User agent is required for SEC EDGAR API
    private const string UserAgentValue = "InsightVest Financial Analysis App 1.0";
    
    public SecEdgarService(
        HttpClient httpClient,
        ICacheService cacheService,
        ILogger<SecEdgarService> logger)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _logger = logger;
        
        // Set required headers for SEC EDGAR API
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgentValue);
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
    }
    
    /// <summary>
    /// Gets company information by ticker
    /// </summary>
    public async Task<(string CikNumber, string CompanyName)> GetCompanyInfoAsync(string ticker, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"sec_company_info_{ticker}";
            var cachedInfo = await _cacheService.GetAsync<(string, string)>(cacheKey);
            if (!string.IsNullOrEmpty(cachedInfo.Item1))
            {
                _logger.LogInformation("Retrieved SEC company info for {Ticker} from cache", ticker);
                return cachedInfo;
            }
            
            // Get CIK number from ticker
            var cikLookupResponse = await _httpClient.GetStringAsync(CikLookupUrl, cancellationToken);
            var lookupData = JsonConvert.DeserializeObject<Dictionary<string, CikData>>(cikLookupResponse);
            
            if (lookupData == null)
            {
                throw new InvalidOperationException("Failed to retrieve CIK lookup data");
            }
            
            var companyData = lookupData.Values.FirstOrDefault(c => c.Ticker?.Equals(ticker, StringComparison.OrdinalIgnoreCase) == true);
            
            if (companyData == null || string.IsNullOrEmpty(companyData.CikStr))
            {
                throw new InvalidOperationException($"Could not find CIK for ticker {ticker}");
            }
            
            var cikNumber = companyData.CikStr;
            var companyName = companyData.Title ?? ticker;
            
            // Cache the result for 7 days (CIK numbers rarely change)
            var companyInfo = (cikNumber, companyName);
            await _cacheService.SetAsync(cacheKey, companyInfo, TimeSpan.FromDays(7));
            
            _logger.LogInformation("Retrieved and cached SEC company info for {Ticker}", ticker);
            return companyInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company info for {Ticker}", ticker);
            throw;
        }
    }
    
    /// <summary>
    /// Gets the latest filings for a company by CIK
    /// </summary>
    public async Task<IEnumerable<SecFiling>> GetCompanyFilingsAsync(
        string cikNumber, 
        IEnumerable<FilingType>? filingTypes = null, 
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var filingTypesKey = filingTypes != null ? string.Join("_", filingTypes) : "all";
            var cacheKey = $"sec_filings_{cikNumber}_{filingTypesKey}_{limit}";
            var cachedFilings = await _cacheService.GetAsync<List<SecFiling>>(cacheKey);
            if (cachedFilings != null && cachedFilings.Any())
            {
                _logger.LogInformation("Retrieved SEC filings for CIK {CikNumber} from cache", cikNumber);
                return cachedFilings;
            }
            
            // Pad CIK number to 10 digits for API
            var paddedCik = cikNumber.PadLeft(10, '0');
            var url = string.Format(CompanyUrl, paddedCik);
            
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var companyData = JsonConvert.DeserializeObject<CompanyInfoResponse>(response);
            
            if (companyData == null)
            {
                throw new InvalidOperationException($"Failed to retrieve company data for CIK {cikNumber}");
            }
            
            // Get ticker from company data
            var ticker = companyData.Tickers?.FirstOrDefault() ?? "";
            var companyName = companyData.Name ?? "";
            
            // Create a placeholder company ID (would be replaced with actual DB ID in a real app)
            var companyId = Guid.NewGuid();
            
            var filingsList = new List<SecFiling>();
            
            // If the company has recent filings, process them
            if (companyData.Filings?.Recent != null)
            {
                var recentFilings = companyData.Filings.Recent;
                
                // Make sure all lists are of the same length
                var count = Math.Min(
                    Math.Min(
                        recentFilings.AccessionNumber?.Count ?? 0,
                        recentFilings.FilingDate?.Count ?? 0),
                    recentFilings.Form?.Count ?? 0);
                
                for (int i = 0; i < count && filingsList.Count < limit; i++)
                {
                    var form = recentFilings.Form?[i] ?? "";
                    var filingType = GetFilingType(form);
                    
                    // Filter by filing type if specified
                    if (filingTypes != null && !filingTypes.Contains(filingType))
                    {
                        continue;
                    }
                    
                    var accessionNumber = recentFilings.AccessionNumber?[i] ?? "";
                    var filingDate = recentFilings.FilingDate != null && i < recentFilings.FilingDate.Count
                        ? DateTime.TryParse(recentFilings.FilingDate[i], out var date) ? date : DateTime.Now
                        : DateTime.Now;
                    
                    var reportDate = recentFilings.ReportDate != null && i < recentFilings.ReportDate.Count
                        ? DateTime.TryParse(recentFilings.ReportDate[i], out var reportDateTime) ? reportDateTime : filingDate
                        : filingDate;
                    
                    var primaryDocument = recentFilings.PrimaryDocument != null && i < recentFilings.PrimaryDocument.Count
                        ? recentFilings.PrimaryDocument[i]
                        : "";
                    
                    // Construct filing URL
                    var filingUrl = $"{BaseUrl}/Archives/edgar/data/{cikNumber}/{accessionNumber.Replace("-", "")}/{primaryDocument}";
                    
                    filingsList.Add(new SecFiling(
                        companyId,
                        ticker,
                        companyName,
                        cikNumber,
                        filingType,
                        form,
                        accessionNumber,
                        filingDate,
                        reportDate,
                        filingUrl
                    ));
                }
            }
            
            // Cache the result for 6 hours (refreshed regularly but not too often)
            await _cacheService.SetAsync(cacheKey, filingsList, TimeSpan.FromHours(6));
            
            _logger.LogInformation("Retrieved {Count} SEC filings for CIK {CikNumber}", filingsList.Count, cikNumber);
            return filingsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company filings for CIK {CikNumber}", cikNumber);
            throw;
        }
    }
    
    /// <summary>
    /// Gets detailed information about a specific filing
    /// </summary>
    public async Task<SecFiling> GetFilingDetailsAsync(
        string accessionNumber, 
        string cikNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"sec_filing_detail_{accessionNumber}";
            var cachedFiling = await _cacheService.GetAsync<SecFiling>(cacheKey);
            if (cachedFiling != null)
            {
                _logger.LogInformation("Retrieved SEC filing details for {AccessionNumber} from cache", accessionNumber);
                return cachedFiling;
            }
            
            // Get all filings and find the one we want
            var filings = await GetCompanyFilingsAsync(cikNumber, null, 100, cancellationToken);
            var filing = filings.FirstOrDefault(f => f.AccessionNumber == accessionNumber);
            
            if (filing == null)
            {
                throw new InvalidOperationException($"Could not find filing {accessionNumber} for CIK {cikNumber}");
            }
            
            // Parse the filing page to find document URLs
            var filingContent = await _httpClient.GetStringAsync(filing.FilingUrl, cancellationToken);
            
            // Use HtmlAgilityPack to parse the HTML
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(filingContent);
            
            // Find the tables that might contain document links
            var tables = htmlDoc.DocumentNode.SelectNodes("//table");
            
            if (tables != null)
            {
                foreach (var table in tables)
                {
                    var rows = table.SelectNodes(".//tr");
                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
                            var cells = row.SelectNodes(".//td");
                            if (cells != null && cells.Count >= 3)
                            {
                                var description = cells[1]?.InnerText.Trim();
                                var documentLink = cells[2]?.SelectSingleNode(".//a");
                                
                                if (documentLink != null)
                                {
                                    var href = documentLink.GetAttributeValue("href", "");
                                    
                                    if (!string.IsNullOrEmpty(href))
                                    {
                                        var documentsUrl = $"{BaseUrl}{href}";
                                        
                                        // Check if it's an HTML document (usually 10-K or 10-Q)
                                        if (href.EndsWith(".htm") || href.EndsWith(".html"))
                                        {
                                            filing.UpdateUrls(documentsUrl, htmlUrl: documentsUrl);
                                        }
                                        // Check if it's a plain text document
                                        else if (href.EndsWith(".txt"))
                                        {
                                            filing.UpdateUrls(documentsUrl, textUrl: documentsUrl);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // Cache the result for 7 days (filings don't change)
            await _cacheService.SetAsync(cacheKey, filing, TimeSpan.FromDays(7));
            
            _logger.LogInformation("Retrieved and cached SEC filing details for {AccessionNumber}", accessionNumber);
            return filing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving filing details for {AccessionNumber}", accessionNumber);
            throw;
        }
    }
    
    /// <summary>
    /// Downloads the content of a filing
    /// </summary>
    public async Task<string> DownloadFilingContentAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cacheKey = $"sec_filing_content_{url.GetHashCode()}";
            var cachedContent = await _cacheService.GetAsync<string>(cacheKey);
            if (!string.IsNullOrEmpty(cachedContent))
            {
                _logger.LogInformation("Retrieved SEC filing content from cache");
                return cachedContent;
            }
            
            // Download the content
            var content = await _httpClient.GetStringAsync(url, cancellationToken);
            
            // Cache the result for 30 days (filings don't change)
            await _cacheService.SetAsync(cacheKey, content, TimeSpan.FromDays(30));
            
            _logger.LogInformation("Downloaded and cached SEC filing content");
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading filing content");
            throw;
        }
    }
    
    /// <summary>
    /// Extracts financial data from a filing document
    /// </summary>
    public async Task<IEnumerable<FinancialStatement>> ExtractFinancialDataAsync(
        string filingContent,
        FilingType filingType,
        CancellationToken cancellationToken = default)
    {
        // Note: This is a placeholder implementation
        // Actual implementation would require complex parsing logic for XBRL data
        // or use of advanced extraction techniques to parse tables from HTML/text
        
        _logger.LogInformation("Extracting financial data from filing");
        
        // In a real implementation, this would analyze the content and extract
        // financial statements and data points
        
        // Return an empty list for now
        return new List<FinancialStatement>();
    }
    
    #region Private Methods
    
    private FilingType GetFilingType(string formType)
    {
        return formType switch
        {
            "10-K" => FilingType.Form10K,
            "10-Q" => FilingType.Form10Q,
            "8-K" => FilingType.Form8K,
            "4" => FilingType.Form4,
            "DEF 14A" => FilingType.FormDef14A,
            _ => FilingType.Other
        };
    }
    
    #endregion
} 