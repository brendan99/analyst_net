using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kamaq.Finsights.Application.Common.Interfaces;
using Kamaq.Finsights.Domain.Entities;
using Kamaq.Finsights.Infrastructure.External.Models.SecEdgar;
using Kamaq.Finsights.Infrastructure.External.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Newtonsoft.Json;
using Xunit;

namespace Kamaq.Finsights.Infrastructure.Tests.External.Services;

public class SecEdgarServiceTests
{
    private const string BaseUrl = "https://data.sec.gov";
    private const string CikLookupUrl = BaseUrl + "/files/company_tickers.json";
    private const string CompanyUrl = BaseUrl + "/submissions/CIK{0}.json";
    
    // Create a mock SEC Edgar service for testing that overrides the constructor behavior
    // to avoid the HttpRequestHeaders issue
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
        
        // Override the methods to avoid using the headers
        public new Task<(string CikNumber, string CompanyName)> GetCompanyInfoAsync(
            string ticker, CancellationToken cancellationToken = default)
        {
            return base.GetCompanyInfoAsync(ticker, cancellationToken);
        }
        
        public new Task<IEnumerable<SecFiling>> GetCompanyFilingsAsync(
            string cikNumber, 
            IEnumerable<FilingType>? filingTypes = null, 
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            return base.GetCompanyFilingsAsync(cikNumber, filingTypes, limit, cancellationToken);
        }
    }

    [Fact]
    public async Task GetCompanyFilingsAsync_ShouldReturnForm10K_ForMicrosoftTicker()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        var httpClient = handler.CreateClient();

        var mockCacheService = new Mock<ICacheService>();
        mockCacheService.Setup(x => x.GetAsync<List<SecFiling>>(It.IsAny<string>()))
            .ReturnsAsync((List<SecFiling>?)null);

        var mockLogger = new Mock<ILogger<SecEdgarService>>();

        // Setup CIK lookup response
        var cikLookupResponse = new Dictionary<string, CikData>
        {
            { "0", new CikData { CikStr = "789019", Ticker = "MSFT", Title = "MICROSOFT CORP" } }
        };
        
        handler
            .SetupRequest(HttpMethod.Get, CikLookupUrl)
            .ReturnsResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(cikLookupResponse));

        // Setup Company Info response with 10-K filings
        var companyInfoUrl = string.Format(CompanyUrl, "0000789019");
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

        var secEdgarService = new TestableSecEdgarService(httpClient, mockCacheService.Object, mockLogger.Object);

        // Act
        var filings = await secEdgarService.GetCompanyFilingsAsync("789019", new[] { FilingType.Form10K }, 5);

        // Assert
        Assert.NotNull(filings);
        Assert.NotEmpty(filings);
        var filing = filings.First();
        Assert.Equal(FilingType.Form10K, filing.FilingType);
        Assert.Equal("MSFT", filing.Ticker);
        Assert.Equal("MICROSOFT CORP", filing.CompanyName);
        Assert.Equal("10-K", filing.FilingTypeDescription);
        Assert.Equal("0001193125-23-221456", filing.AccessionNumber);
        Assert.Contains("/Archives/edgar/data/789019/", filing.FilingUrl);
    }

    [Fact]
    public async Task GetCompanyInfoAsync_ShouldReturnCikNumberAndCompanyName_ForMicrosoftTicker()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        var httpClient = handler.CreateClient();

        var mockCacheService = new Mock<ICacheService>();
        mockCacheService.Setup(x => x.GetAsync<(string, string)>(It.IsAny<string>()))
            .ReturnsAsync((string.Empty, string.Empty));

        var mockLogger = new Mock<ILogger<SecEdgarService>>();

        // Setup CIK lookup response
        var cikLookupResponse = new Dictionary<string, CikData>
        {
            { "0", new CikData { CikStr = "789019", Ticker = "MSFT", Title = "MICROSOFT CORP" } }
        };

        handler
            .SetupRequest(HttpMethod.Get, CikLookupUrl)
            .ReturnsResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(cikLookupResponse));

        var secEdgarService = new TestableSecEdgarService(httpClient, mockCacheService.Object, mockLogger.Object);

        // Act
        var result = await secEdgarService.GetCompanyInfoAsync("MSFT");

        // Assert
        Assert.Equal("789019", result.CikNumber);
        Assert.Equal("MICROSOFT CORP", result.CompanyName);
    }
} 