using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kamaq.Finsights.Application.Common.Interfaces;
using Kamaq.Finsights.Application.Common.Interfaces.ExternalServices;
using Kamaq.Finsights.Domain.Entities;
using Kamaq.Finsights.Infrastructure.External.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kamaq.Finsights.Infrastructure.Tests.External.Services;

public class DataAggregatorTests
{
    [Fact]
    public async Task GetSecFilingsAsync_ShouldReturnForm10K_ForMicrosoftTicker()
    {
        // Arrange
        var mockYahooFinanceService = new Mock<IYahooFinanceService>();
        var mockSecEdgarService = new Mock<ISecEdgarService>();
        var mockCacheService = new Mock<ICacheService>();
        var mockLogger = new Mock<ILogger<DataAggregator>>();

        // Setup SEC Edgar service to return CIK for MSFT
        mockSecEdgarService.Setup(x => x.GetCompanyInfoAsync("MSFT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("789019", "MICROSOFT CORP"));

        // Setup 10-K filings
        var secFilings = new List<SecFiling>
        {
            new SecFiling(
                Guid.NewGuid(),
                "MSFT",
                "MICROSOFT CORP",
                "789019",
                FilingType.Form10K,
                "10-K",
                "0001193125-23-221456",
                new DateTime(2023, 7, 27),
                new DateTime(2023, 6, 30),
                "https://data.sec.gov/Archives/edgar/data/789019/000119312523221456/msft-20230630.htm"
            )
        };

        mockSecEdgarService.Setup(x => x.GetCompanyFilingsAsync(
                "789019", 
                It.Is<IEnumerable<FilingType>>(ft => ft != null && ft.Contains(FilingType.Form10K)),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(secFilings);

        var dataAggregator = new DataAggregator(
            mockYahooFinanceService.Object,
            mockSecEdgarService.Object,
            mockCacheService.Object,
            mockLogger.Object);

        // Act
        var result = await dataAggregator.GetSecFilingsAsync(
            "MSFT", 
            new[] { FilingType.Form10K },
            10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var filing = result.First();
        Assert.Equal(FilingType.Form10K, filing.FilingType);
        Assert.Equal("MSFT", filing.Ticker);
        Assert.Equal("10-K", filing.FilingTypeDescription);
        Assert.Equal("0001193125-23-221456", filing.AccessionNumber);
        Assert.Contains("789019", filing.FilingUrl);

        // Verify service calls
        mockSecEdgarService.Verify(
            x => x.GetCompanyInfoAsync("MSFT", It.IsAny<CancellationToken>()),
            Times.Once);

        mockSecEdgarService.Verify(
            x => x.GetCompanyFilingsAsync(
                "789019", 
                It.Is<IEnumerable<FilingType>>(ft => ft != null && ft.Contains(FilingType.Form10K)),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFinancialStatementsAsync_ShouldDownloadAndExtractData_FromSecFiling()
    {
        // Arrange
        var mockYahooFinanceService = new Mock<IYahooFinanceService>();
        var mockSecEdgarService = new Mock<ISecEdgarService>();
        var mockCacheService = new Mock<ICacheService>();
        var mockLogger = new Mock<ILogger<DataAggregator>>();

        // Create a filing with no HTML or text URLs
        var filing = new SecFiling(
            Guid.NewGuid(),
            "MSFT",
            "MICROSOFT CORP",
            "789019",
            FilingType.Form10K,
            "10-K",
            "0001193125-23-221456",
            new DateTime(2023, 7, 27),
            new DateTime(2023, 6, 30),
            "https://data.sec.gov/Archives/edgar/data/789019/000119312523221456/0001193125-23-221456-index.htm"
        );

        // Setup filing details with URLs
        var filingWithUrls = new SecFiling(
            filing.CompanyId,
            filing.Ticker,
            filing.CompanyName,
            filing.CikNumber,
            filing.FilingType,
            filing.FilingTypeDescription,
            filing.AccessionNumber,
            filing.FilingDate,
            filing.ReportDate,
            filing.FilingUrl,
            "https://data.sec.gov/Archives/edgar/data/789019/000119312523221456/",
            "https://data.sec.gov/Archives/edgar/data/789019/000119312523221456/msft-20230630.htm",
            null
        );

        // Setup file content
        const string fileContent = "<html><body>Financial data...</body></html>";

        // Setup financial statements
        var statements = new List<FinancialStatement>
        {
            new FinancialStatement(
                filing.CompanyId,
                filing.Ticker,
                StatementType.IncomeStatement,
                ReportingPeriod.Annual,
                filing.FilingDate,
                filing.ReportDate,
                filing.FilingUrl,
                filing.AccessionNumber,
                "2023",
                null
            )
        };

        // Set up the mocks
        mockSecEdgarService.Setup(x => x.GetFilingDetailsAsync(
                filing.AccessionNumber, 
                filing.CikNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(filingWithUrls);

        mockSecEdgarService.Setup(x => x.DownloadFilingContentAsync(
                filingWithUrls.HtmlUrl!,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        mockSecEdgarService.Setup(x => x.ExtractFinancialDataAsync(
                fileContent,
                filing.FilingType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(statements);

        var dataAggregator = new DataAggregator(
            mockYahooFinanceService.Object,
            mockSecEdgarService.Object,
            mockCacheService.Object,
            mockLogger.Object);

        // Act
        var result = await dataAggregator.GetFinancialStatementsAsync(filing);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var statement = result.First();
        Assert.Equal(StatementType.IncomeStatement, statement.StatementType);
        Assert.Equal(ReportingPeriod.Annual, statement.Period);
        Assert.Equal(filing.Ticker, statement.Ticker);
        Assert.Equal(filing.AccessionNumber, statement.AccessionNumber);

        // Verify interactions
        mockSecEdgarService.Verify(
            x => x.GetFilingDetailsAsync(
                filing.AccessionNumber,
                filing.CikNumber,
                It.IsAny<CancellationToken>()),
            Times.Once);

        mockSecEdgarService.Verify(
            x => x.DownloadFilingContentAsync(
                filingWithUrls.HtmlUrl!,
                It.IsAny<CancellationToken>()),
            Times.Once);

        mockSecEdgarService.Verify(
            x => x.ExtractFinancialDataAsync(
                fileContent,
                filing.FilingType,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
} 