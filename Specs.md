Okay, let's revise the backend specification for **Kamaq Insights**, incorporating a message bus (like RabbitMQ or Azure Service Bus) using an abstraction library (like MassTransit or NServiceBus) to handle long-running background tasks asynchronously, following Clean Architecture principles.

**Project Goal:** To build a robust, maintainable, and scalable backend service for the Kamaq Insights application, leveraging Clean Architecture, MediatR for in-process communication, and a message bus for decoupling and processing long-running tasks like SEC 10-K form analysis.

**Core Technologies & Libraries:**

*   **Framework:** ASP.NET Core (latest LTS or stable version)
*   **Language:** C#
*   **Architecture:** Clean Architecture
*   **In-Process Messaging:** MediatR
*   **Inter-Process Messaging (Message Bus):**
    *   **Abstraction:** MassTransit (recommended) or NServiceBus
    *   **Broker:** RabbitMQ (good for cross-platform/on-prem) or Azure Service Bus (good for Azure ecosystem)
*   **Validation:** FluentValidation
*   **Mapping:** AutoMapper or Mapster
*   **Database ORM:** Entity Framework Core (EF Core)
*   **Database:** PostgreSQL or SQL Server (Primary); Optional Time-series DB
*   **HTTP Client:** `IHttpClientFactory` with Polly
*   **Background Task Host:** .NET Worker Service project template
*   **Caching:** `IMemoryCache` or Redis (`StackExchange.Redis`)
*   **Logging:** Serilog or NLog
*   **API Documentation:** Swashbuckle (Swagger)
*   **Testing:** xUnit/NUnit, Moq/NSubstitute, FluentAssertions, Testcontainers, `MassTransit.Testing`

**Clean Architecture Project Structure:**

```
/KamaqInsights.sln
|
├─── src
│    ├─── KamaqInsights.Domain          # Entities, Value Objects, Enums, Domain Events
│    ├─── KamaqInsights.Application     # Use Cases (Commands/Queries/Events), Interfaces, DTOs, Validation, Mapping
│    ├─── KamaqInsights.Infrastructure  # Data Access, API Clients, Caching, Message Bus Impl., Analysis Logic
│    ├─── KamaqInsights.Api             # ASP.NET Core Controllers, API DTOs, Middleware, Startup
│    └─── KamaqInsights.Worker          # .NET Worker Service hosting Message Bus Consumers
│
└─── tests
     ├─── KamaqInsights.Domain.Tests
     ├─── KamaqInsights.Application.Tests
     ├─── KamaqInsights.Infrastructure.Tests
     ├─── KamaqInsights.Api.Tests
     └─── KamaqInsights.Worker.Tests      # Tests for message consumers
```

**Layer Breakdown & Message Bus Integration:**

1.  **`KamaqInsights.Domain`**
    *   Entities, Value Objects, Enums (as before).
    *   Domain Events: Can still be used for *in-process* notifications via MediatR. Might define events related to triggering background tasks (e.g., `TenKAnalysisRequested`), but the actual *sending* happens in Application/Infrastructure.

2.  **`KamaqInsights.Application`**
    *   **Features (Commands/Queries/Event Handlers):**
        *   `Stocks/Commands/RequestTenKAnalysis/`:
            *   `RequestTenKAnalysisCommand.cs`: MediatR `IRequest`. Contains `Ticker`, potentially date range or specific filing IDs.
            *   `RequestTenKAnalysisCommandHandler.cs`: Implements `IRequestHandler<RequestTenKAnalysisCommand>`.
                *   Injects `IMessageBusPublisher` (defined below).
                *   Validates the command.
                *   Creates a specific message contract (e.g., `AnalyzeTenKMessage`).
                *   Uses `_messageBusPublisher.Publish(analyzeTenKMessage)` or `_messageBusPublisher.Send(analyzeTenKMessage)` (depending on pattern - Publish/Subscribe vs Send/Receive).
                *   **Crucially:** Does *not* perform the long-running task itself. Returns quickly.
        *   Other Queries/Commands remain largely the same, using MediatR for in-process orchestration.
    *   **Interfaces (Abstractions):**
        *   `Persistence/IApplicationDbContext.cs`
        *   `ExternalServices/IFinancialDataClient.cs`
        *   `ExternalServices/ISecEdgarClient.cs`: Interface for interacting with SEC EDGAR database (finding filings, download links).
        *   `Analysis/IFormParser.cs`: Interface for parsing downloaded forms (e.g., 10-K).
        *   `Analysis/ITextAnalyzer.cs`: Interface for extracting insights from parsed text (sentiment, keyword extraction, risk factor changes etc.).
        *   `Caching/ICacheService.cs`
        *   **`Messaging/IMessageBusPublisher.cs`**: Defines the contract for sending/publishing messages to the bus (e.g., `Task Publish<T>(T message) where T : class;`, `Task Send<T>(T message) where T : class;`).
    *   **Message Contracts:** Define the structure of messages sent over the bus. These might live in Application or a dedicated shared `KamaqInsights.Contracts` library referenced by both `Api`/`Application` and `Worker`.
        *   `AnalyzeTenKMessage.cs`: { string Ticker, string AccessionNumber, string FilingUrl }
        *   `TenKAnalysisCompletedMessage.cs`: { string Ticker, string AccessionNumber, bool Success, string Summary, List<ExtractedDataPoint> DataPoints }
        *   `TenKAnalysisFailedMessage.cs`: { string Ticker, string AccessionNumber, string ErrorMessage }
    *   **Common:** Mappings, Behaviors (Validation, Logging for MediatR), Exceptions.
    *   **Dependencies:** `Domain`.

3.  **`KamaqInsights.Infrastructure`**
    *   **Persistence:** `ApplicationDbContext`, Repositories (optional).
    *   **External Services:** Implementations for `IFinancialDataClient`, `ISecEdgarClient`.
    *   **Analysis:** Implementations for `IFormParser`, `ITextAnalyzer`. This is where the heavy lifting of parsing and analyzing the 10-K content happens.
    *   **Caching:** Implementations for `ICacheService`.
    *   **Messaging:**
        *   `MassTransitPublisher.cs` / `NServiceBusPublisher.cs`: Implementation of `IMessageBusPublisher` using the chosen library's API (`IPublishEndpoint`, `ISendEndpointProvider` for MassTransit or `IMessageSession` for NServiceBus). Configured via DI.
    *   **DependencyInjection.cs:** Registers infrastructure services, including message bus configuration (connection string, endpoint mappings).
    *   **Dependencies:** `Application`.

4.  **`KamaqInsights.Api`**
    *   **Controllers:** Thin controllers injecting `ISender` (MediatR).
        *   Endpoints trigger MediatR commands (like `RequestTenKAnalysisCommand`).
        *   Return immediate responses (e.g., HTTP 202 Accepted) indicating the task has been queued.
        *   Might have endpoints to check the status of background tasks if status tracking is implemented.
    *   **Middleware:** Error handling, logging.
    *   `Program.cs` / `Startup.cs`: Configure services (DI), including MediatR, message bus *publishing* setup (connecting to broker), Swagger, etc. **Does not configure consumers.**
    *   **Dependencies:** `Application`, `Infrastructure` (for DI setup).

5.  **`KamaqInsights.Worker` (.NET Worker Service)**
    *   **Purpose:** Hosts the message bus *consumers* that perform the long-running tasks. Runs as a separate process.
    *   **Consumers:**
        *   `AnalyzeTenKConsumer.cs`: Implements `IConsumer<AnalyzeTenKMessage>` (MassTransit) or inherits `IHandleMessages<AnalyzeTenKMessage>` (NServiceBus).
            *   Injects necessary infrastructure services: `ISecEdgarClient`, `IFormParser`, `ITextAnalyzer`, `IApplicationDbContext`, `IMessageBusPublisher` (to potentially publish completion/failure events), `ILogger`.
            *   `Consume` / `Handle` method:
                1.  Receives `AnalyzeTenKMessage`.
                2.  Logs start of processing.
                3.  Uses `ISecEdgarClient` to download the specified form.
                4.  Uses `IFormParser` to parse the downloaded content.
                5.  Uses `ITextAnalyzer` to extract insights.
                6.  Uses `IApplicationDbContext` to save results to the database.
                7.  Publishes `TenKAnalysisCompletedMessage` or `TenKAnalysisFailedMessage` via `IMessageBusPublisher`.
                8.  Logs completion/failure.
            *   Includes robust error handling (try/catch), retry logic (often configured via MassTransit/NServiceBus), and dead-lettering.
    *   `Program.cs` (Worker Service):
        *   Configures Dependency Injection, registering necessary services from `Infrastructure` and potentially `Application` (interfaces).
        *   Configures MassTransit/NServiceBus:
            *   Connects to the message broker (RabbitMQ/Azure Service Bus).
            *   **Registers consumers** (e.g., `AnalyzeTenKConsumer`).
            *   Defines endpoints/queues.
            *   Configures retry policies, concurrency limits, etc.
        *   Builds and runs the host (`IHost`).
    *   **Dependencies:** `Application`, `Infrastructure`, `Domain`.

**Workflow Example (10-K Analysis Request):**

1.  User triggers action in UI -> `POST /api/stocks/AAPL/request-10k-analysis` request to `KamaqInsights.Api`.
2.  `StocksController` receives request.
3.  Controller creates `RequestTenKAnalysisCommand { Ticker = "AAPL", ... }`.
4.  Controller calls `await _mediator.Send(command);`.
5.  MediatR routes to `RequestTenKAnalysisCommandHandler` in `Application`.
6.  Handler validates, creates `AnalyzeTenKMessage { Ticker = "AAPL", ... }`.
7.  Handler calls `await _messageBusPublisher.Publish(message);` (using the implementation from `Infrastructure`).
8.  The message is sent to the configured RabbitMQ exchange / Azure Service Bus topic.
9.  Handler returns success quickly.
10. API Controller returns `HTTP 202 Accepted`.
11. **Meanwhile, in `KamaqInsights.Worker` service:**
12. MassTransit/NServiceBus is listening to the queue bound to the exchange/topic.
13. It receives the `AnalyzeTenKMessage`.
14. It instantiates and invokes the `Consume`/`Handle` method on `AnalyzeTenKConsumer`.
15. `AnalyzeTenKConsumer` performs the download, parsing, analysis (using injected infrastructure services). This may take seconds or minutes.
16. Consumer saves results to the database via `IApplicationDbContext`.
17. Consumer publishes `TenKAnalysisCompletedMessage` back to the bus.
18. (Optional) Another consumer in the `Api` or `Worker` could listen for `TenKAnalysisCompletedMessage` to update status or notify the user (e.g., via SignalR).

**Benefits of this Approach:**

*   **Responsiveness:** API remains responsive as long-running tasks are offloaded.
*   **Scalability:** Worker services can be scaled independently of the API based on the load of background tasks.
*   **Resilience:** Message bus provides retries and dead-lettering, making background processing more robust to transient failures.
*   **Decoupling:** API doesn't need to know *how* the analysis is done, only that it needs to be requested. Worker doesn't need to know about the API.
*   **Maintainability:** Clear separation of concerns according to Clean Architecture.

**Considerations:**

*   **Message Contract Versioning:** Plan for how to handle changes to message contracts over time.
*   **Idempotency:** Consumers should ideally be idempotent (processing the same message multiple times has the same effect as processing it once).
*   **Monitoring:** Implement monitoring for queue depths, consumer health, and processing times/errors.
*   **Distributed Transactions:** Avoid distributed transactions if possible; use eventual consistency patterns. Compensating actions might be needed for failures.
*   **Complexity:** Introduces the operational complexity of managing a message broker and separate worker services.