using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kamaq.Finsights.Application.Common.Interfaces;
using Kamaq.Finsights.Application.Common.Interfaces.ExternalServices;
using Kamaq.Finsights.Domain.Entities;
using Kamaq.Finsights.Infrastructure.External.Models.SecEdgar;
using Kamaq.Finsights.Infrastructure.External.Models.YahooFinance;
using Kamaq.Finsights.Infrastructure.External.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Newtonsoft.Json;
using Xunit;

namespace Kamaq.Finsights.Infrastructure.Tests.External.Services;

/// <summary>
/// Integration tests for Yahoo Finance and SEC Edgar services working together
/// via the DataAggregator
/// </summary>
public class YahooSecIntegrationTests
{
    private const string YahooBaseUrl = "https://query1.finance.yahoo.com/v8/finance";
    private const string YahooModulesUrl = YahooBaseUrl + "/quoteSummary";
    private const string SecBaseUrl = "https://data.sec.gov";
    private const string SecCikLookupUrl = SecBaseUrl + "/files/company_tickers.json";
    private const string SecCompanyUrl = SecBaseUrl + "/submissions/CIK{0}.json";
    
    // Create testable versions of the services to avoid the HttpRequestHeaders issue
    private class TestableSecEdgarService : SecEdgarService
    {
        public TestableSecEdgarService(
            HttpClient httpClient,
            ICacheService cacheService,
            ILogger<SecEdgarService> logger) 
            : base(httpClient, cacheService, logger)
        {
            // Intentionally empty - the base constructor will be called but we won't use the headers
        }
    }
    
    [Fact]
    public async Task GetComprehensiveCompanyData_ForMicrosoft_ShouldReturnBothMarketAndFilingData()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        var httpClient = handler.CreateClient();
        var mockCacheService = new Mock<ICacheService>();
        
        // Setup cache misses to force API calls
        mockCacheService.Setup(x => x.GetAsync<Company>(It.IsAny<string>()))
            .ReturnsAsync((Company?)null);
        mockCacheService.Setup(x => x.GetAsync<List<SecFiling>>(It.IsAny<string>()))
            .ReturnsAsync((List<SecFiling>?)null);
        mockCacheService.Setup(x => x.GetAsync<(string, string)>(It.IsAny<string>()))
            .ReturnsAsync((string.Empty, string.Empty));
        mockCacheService.Setup(x => x.GetAsync<(decimal, decimal, decimal, decimal, decimal)>(It.IsAny<string>()))
            .ReturnsAsync((0m, 0m, 0m, 0m, 0m));
        
        // Create loggers
        var mockYahooLogger = new Mock<ILogger<YahooFinanceService>>();
        var mockSecLogger = new Mock<ILogger<SecEdgarService>>();
        var mockAggregatorLogger = new Mock<ILogger<DataAggregator>>();
        
        #region Yahoo Finance API setup
        
        // Setup Yahoo Finance profile response (used for company info)
        var yahooProfileUrl = $"{YahooModulesUrl}/MSFT?modules=assetProfile";
        var assetProfile = new YahooAssetProfile
        {
            AssetProfile = new AssetProfile
            {
                LongBusinessSummary = "Microsoft Corporation develops and supports software, services, devices, and solutions worldwide.",
                Exchange = "NMS",
                Sector = "Technology",
                Industry = "Software—Infrastructure",
                Website = "https://www.microsoft.com"
            }
        };
        
        handler
            .SetupRequest(HttpMethod.Get, yahooProfileUrl)
            .ReturnsResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(assetProfile));
        
        // Setup Yahoo Finance quote response
        var yahooQuoteUrl = $"{YahooBaseUrl}/quote?symbols=MSFT";
        var quoteResponse = new YahooQuoteResponse
        {
            QuoteResponse = new QuoteResponseData
            {
                Result = new List<QuoteResult>
                {
                    new QuoteResult
                    {
                        Symbol = "MSFT",
                        RegularMarketPrice = 420.75m,
                        RegularMarketChange = 2.50m,
                        RegularMarketChangePercent = 0.60m,
                        RegularMarketVolume = 18500000,
                        MarketCap = 3125000000000m
                    }
                }
            }
        };
        
        handler
            .SetupRequest(HttpMethod.Get, yahooQuoteUrl)
            .ReturnsResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(quoteResponse));
        
        #endregion
        
        #region SEC Edgar API setup
        
        // Setup SEC CIK lookup response
        var cikLookupResponse = new Dictionary<string, CikData>
        {
            { "0", new CikData { CikStr = "789019", Ticker = "MSFT", Title = "MICROSOFT CORP" } }
        };
        
        handler
            .SetupRequest(HttpMethod.Get, SecCikLookupUrl)
            .ReturnsResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(cikLookupResponse));
        
        // Setup SEC Company Info response with 10-K filings
        var companyInfoUrl = string.Format(SecCompanyUrl, "0000789019");
        var recentFilings = new RecentFilings
        {
            AccessionNumber = new List<string> { "0001193125-23-221456" },
            FilingDate = new List<string> { "2023-07-27" },
            ReportDate = new List<string> { "2023-06-30" },
            Form = new List<string> { "10-K" },
            PrimaryDocument = new List<string> { "msft-20230630.htm" }
        };
        
        var companyInfo = new CompanyInfoResponse
        {
            Cik = "789019",
            Name = "MICROSOFT CORP",
            Tickers = new List<string> { "MSFT" },
            Filings = new FilingsData
            {
                Recent = recentFilings
            }
        };
        
        handler
            .SetupRequest(HttpMethod.Get, companyInfoUrl)
            .ReturnsResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(companyInfo));
        
        #endregion
        
        // Create the real services with mocked dependencies
        var yahooService = new YahooFinanceService(httpClient, mockCacheService.Object, mockYahooLogger.Object);
        var secService = new TestableSecEdgarService(httpClient, mockCacheService.Object, mockSecLogger.Object);
        
        // Create the data aggregator (the service under test)
        var dataAggregator = new DataAggregator(
            yahooService,
            secService,
            mockCacheService.Object,
            mockAggregatorLogger.Object);
        
        // Act
        
        // 1. Get company profile from Yahoo with CIK from SEC
        var company = await dataAggregator.GetCompanyProfileAsync("MSFT");
        
        // 2. Get SEC filings
        var filings = await dataAggregator.GetSecFilingsAsync("MSFT", new[] { FilingType.Form10K }, 5);
        
        // Assert
        
        // Company profile assertions
        Assert.NotNull(company);
        Assert.Equal("MSFT", company.Ticker);
        Assert.Contains("Microsoft", company.Name);
        Assert.Equal("NMS", company.Exchange);
        Assert.Equal("Technology", company.Sector);
        Assert.Equal("Software—Infrastructure", company.Industry);
        Assert.Equal("789019", company.CikNumber);
        Assert.Equal("https://www.microsoft.com", company.Website);
        Assert.True(company.MarketCap > 0);
        
        // SEC filings assertions
        Assert.NotNull(filings);
        Assert.NotEmpty(filings);
        var filing = filings.First();
        Assert.Equal(FilingType.Form10K, filing.FilingType);
        Assert.Equal("10-K", filing.FilingTypeDescription);
        Assert.Equal("0001193125-23-221456", filing.AccessionNumber);
        Assert.Contains("789019", filing.FilingUrl);
        Assert.Equal(new DateTime(2023, 7, 27), filing.FilingDate);
        Assert.Equal(new DateTime(2023, 6, 30), filing.ReportDate);
    }
} 