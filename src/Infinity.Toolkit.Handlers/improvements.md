# Infinity.Toolkit.Handlers — Production Readiness Tasks

This document lists the work needed to make the in-memory message queue in `Infinity.Toolkit.Handlers` production ready. Each task is self-contained enough to be implemented independently. Dependencies between tasks are listed explicitly.

## Context

### Current architecture

All types currently live in one source file in namespace `Infinity.Toolkit.Handlers`:

| Type | Visibility | Role |
|---|---|---|
| `InMemoryMessageQueueExtensions.AddInMemoryMessageQueue` | public | DI registration; uses `TryAdd*`; returns `IServiceCollection` |
| `InMemoryMessageQueue` | internal | Wraps an unbounded `Channel<IEvent>` (`SingleReader = true`) |
| `IEvent` | public | Event contract; exposes `string EventType` |
| `IEventBus` / `EventBus` | public / internal | Producer API; `PublishAsync<T>` writes the event to the channel; null-guarded |
| `EventProcessorJob` | internal | `BackgroundService`; reads the channel, creates one async DI scope per event, dispatches via `EventDispatcher` |
| `EventDispatcher` / `EventDispatcher<TEvent>` | internal | Closes the generic over the event's **runtime** type (cached via `MakeGenericType`), then calls `IEventPublisher.PublishAsync<TEvent>` |
| `IEventPublisher` / `EventPublisher` | internal | Resolves all `IEventHandler<TEvent>` from the event's scope and runs them **sequentially**; one failing handler is logged and does not stop the others |
| `IEventHandler<T>` | public | Handler contract; receives `IEventHandlerContext<T>` |
| `IEventHandlerContext<TEvent>` / `EventHandlerContext<TEvent>` | public | Carries `Event` and `Body` (`BinaryData` JSON of the event, built once per event) |

### Design decisions already made (do not revert)

- **Sequential handler execution per event.** Handlers for one event share a DI scope, so scoped dependencies (for example `DbContext`) are shared. Running handlers concurrently would cause concurrent use of non-thread-safe scoped services.
- **Dispatch on runtime type.** The channel erases the static type, so dispatch must use `@event.GetType()`.
- **No `class` constraint.** Events may be structs. Constraints are `where T : IEvent` across the whole chain.
- **Delivery semantics: best-effort.** Losing events on crash, restart or deploy is currently acceptable. Tasks D1–D7 cover the move to guaranteed delivery when that changes.

### Conventions for all tasks

- Target framework is .NET 8 or later (primary constructors, `TimeProvider`, `IMeterFactory`). Verify against the `.csproj` before using newer APIs.
- Use structured logging with message templates only, never interpolated strings. Use property names consistently: `EventType`, `MessageId`, `Handler`.
- **Never log the event payload or `Body`.** Events can contain personal data.
- Configurable behavior goes through the options class from T5.
- Every task includes tests (see T14 for the test infrastructure). If T14 doesn't exist yet, add the tests to the existing test project and move them later.
- Record public API changes in the PR description. Once T15 is done, they are also tracked by PublicApiAnalyzers.

### Implementation order

T0 → T1 → T2 → T3 → T4 → T5 → T6 → T7 → T8 → T9 → T10 → T11 → T12 → T13 (optional) → T14 runs alongside all tasks → T15.

---

## Part 1 — Hardening the in-memory queue

### T0 — Leftover fixes from code review

**Depends on:** nothing. Skip any item that is already done.

**Changes:**
1. Change `EventDispatcher<TEvent>`'s constraint from `where TEvent : class, IEvent` to `where TEvent : IEvent`. Otherwise struct events are accepted by `IEventBus` but fail at runtime in `MakeGenericType` with an `ArgumentException`.
2. Mark `EventHandlerContext<TEvent>.Event` and `.Body` as `required` to remove the CS8618 warnings and prevent half-initialized contexts.
3. Rename the `IEventBus.PublishAsync` parameter from `integrationEvent` to `@event` so it matches the implementation (CA1725).

**Acceptance criteria:**
- [ ] Publishing a `record struct` event reaches its `IEventHandler<T>`.
- [ ] The project builds with no nullable warnings in these types.

---

### T1 — Document delivery guarantees

**Depends on:** nothing.

**Why:** Callers will assume stronger guarantees than an in-memory queue provides unless the guarantees are stated explicitly.

**Changes:** Add a "Delivery guarantees" section to the package README that states:
- **Best-effort delivery.** Events are lost on process crash, restart, deploy, or OOM.
- **Scope.** Each process has its own queue. With multiple replicas, an event is handled only by the instance that published it.
- **Ordering.** Events are processed in publish order, one at a time. T13 changes this when multiple consumers are enabled. Handlers for one event run sequentially in DI registration order, which is not a guaranteed contract.
- **Failure isolation.** A failing handler does not prevent the other handlers of the same event from running.
- **Handler context.** Handlers run outside the HTTP request, so `HttpContext`, the current user, and request-scoped services are not available. Anything a handler needs must be on the event.
- **When not to use this.** Anything where losing an event is unacceptable. Link to Part 2 of this document, or to its successor docs.

**Acceptance criteria:**
- [ ] The README contains all of the points above.

---

### T2 — Event envelope

**Depends on:** T0.

**Why:** Tracing, logging scopes, metrics, retries and shutdown all need per-message metadata that `IEvent` does not carry.

**Changes:**
1. Add an internal envelope type:
   ```csharp
   internal sealed record EventEnvelope(
       IEvent Event,
       string MessageId,
       DateTimeOffset EnqueuedAt,
       ActivityContext ParentContext);
   ```
2. Change `InMemoryMessageQueue` to wrap `Channel<EventEnvelope>`.
3. In `EventBus.PublishAsync`, build the envelope with:
   - `MessageId`: `Guid.NewGuid().ToString("N")`, or `Guid.CreateVersion7()` on .NET 9+ to get time-ordered IDs
   - `EnqueuedAt`: from an injected `TimeProvider`. Register `TimeProvider.System` with `TryAddSingleton` if nothing is registered.
   - `ParentContext`: `Activity.Current?.Context ?? default`. T4 overrides this with the producer span.
4. Update `EventProcessorJob` and `EventDispatcher` to read the envelope and pass `envelope.Event` to the publisher. Pass the envelope itself wherever later tasks need it.
5. Optionally, expose `MessageId` on `IEventHandlerContext<T>` so handlers can use it for idempotency (see T9 and D4). If you add it, update the README.

**Acceptance criteria:**
- [ ] The public API is unchanged, except for the optional `MessageId` on the handler context.
- [ ] A unit test with `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) verifies `EnqueuedAt`.
- [ ] Every published event gets a unique `MessageId`.

---

### T3 — Logging scope per message

**Depends on:** T2.

**Changes:** In `EventProcessorJob`, wrap all processing of one envelope in:
```csharp
using var logScope = logger.BeginScope(new Dictionary<string, object>
{
    ["MessageId"] = envelope.MessageId,
    ["EventType"] = envelope.Event.EventType,
});
```
Open the scope inside the `try`, so a failure to create it cannot escape the loop.

**Acceptance criteria:**
- [ ] A test with a capturing logger provider shows that log entries written by a handler carry `MessageId` and `EventType` scope properties.

---

### T4 — Distributed tracing

**Depends on:** T2.

**Why:** Handlers run on the background loop, where `Activity.Current` is null. Without this task, their database and HTTP calls show up as disconnected root traces.

**Changes:**
1. Add an internal static `ActivitySource` named `Infinity.Toolkit.Handlers`, with the version set to the assembly version.
2. **Producer span.** In `EventBus.PublishAsync`, start `publish {EventType}` with `ActivityKind.Producer`. Store that activity's context in `envelope.ParentContext`. If no listener is attached, the activity is null, so fall back to `Activity.Current?.Context ?? default`.
3. **Consumer span.** In `EventProcessorJob`, for each envelope, start `process {EventType}` with `ActivityKind.Consumer`.
   - The parenting mode is configurable via T5 options: `TraceParenting.Parent` (default) uses `envelope.ParentContext` as the parent; `TraceParenting.Link` starts a new root and adds an `ActivityLink` to `envelope.ParentContext`.
   - Set the tags `messaging.system = "infinity.inmemory"`, `messaging.operation.type = "process"`, `messaging.message.id`, and `infinity.event_type`.
   - On failure, set `ActivityStatusCode.Error` and record the exception with `activity.AddException(ex)` (.NET 9+) or an `exception` event.
4. **Handler spans (optional).** Add one child span per handler named `handle {EventType} {Handler}`. This makes slow handlers visible in the trace.
5. Expose a public constant, for example `InfinityHandlersDiagnostics.ActivitySourceName`, and document that consumers must call `.AddSource(InfinityHandlersDiagnostics.ActivitySourceName)` in their OpenTelemetry setup.

**Acceptance criteria:**
- [ ] A test with an `ActivityListener` shows that the consumer span's parent (or link, in Link mode) equals the producer span published from inside an ambient activity.
- [ ] An activity started inside a handler has the consumer span (or handler span) as its parent.
- [ ] With no listener attached, nothing is allocated and nothing throws.

---

### T5 — Options infrastructure

**Depends on:** nothing. Do this before T6–T13, which add their settings here.

**Changes:**
1. Add a public `InMemoryMessageQueueOptions` class. Settings are added by later tasks; the initial set with defaults:
   - `Capacity` (int, default 10_000) — T6
   - `HandlerTimeout` (TimeSpan, default 30 s; `Timeout.InfiniteTimeSpan` disables it) — T7
   - `RetryCount` (int, default 3), `RetryBaseDelay` (TimeSpan, default 200 ms), `RetryMaxDelay` (TimeSpan, default 10 s) — T9
   - `TraceParenting` (enum `Parent` | `Link`, default `Parent`) — T4
   - `ConsumerCount` (int, default 1) — T13
   - `DegradedQueueLatency` (TimeSpan, default 5 s) — T11
2. Add an overload `AddInMemoryMessageQueue(this IServiceCollection services, Action<InMemoryMessageQueueOptions>? configure = null)`.
3. Register the options with `AddOptions<InMemoryMessageQueueOptions>().Configure(...).Validate(...).ValidateOnStart()`. Validation requires a positive `Capacity`, a non-negative `RetryCount`, a positive `ConsumerCount`, a positive or infinite `HandlerTimeout`, and `RetryBaseDelay <= RetryMaxDelay`.
4. Also support binding from configuration, for example a section `Infinity:Handlers:InMemory`. That can be a second overload or documented `BindConfiguration` usage.

**Acceptance criteria:**
- [ ] Invalid options fail at host startup with a descriptive message.
- [ ] Calling `AddInMemoryMessageQueue` twice does not register duplicate services. Existing idempotency is preserved, and `configure` delegates compose.

---

### T6 — Bounded channel with backpressure

**Depends on:** T2, T5.

**Why:** An unbounded channel turns a slow consumer into unbounded memory growth and eventually an OOM.

**Changes:**
1. Replace `Channel.CreateUnbounded` with `Channel.CreateBounded<EventEnvelope>(new BoundedChannelOptions(options.Capacity) { FullMode = BoundedChannelFullMode.Wait, SingleReader = options.ConsumerCount == 1, SingleWriter = false })`.
2. `InMemoryMessageQueue` must get the capacity from `IOptions<InMemoryMessageQueueOptions>` in its constructor.
3. `EventBus.PublishAsync` already awaits `WriteAsync`, so when the queue is full, producers wait (backpressure) and honor their `CancellationToken`. Document in XML docs that `PublishAsync` can wait when the queue is full.

**Acceptance criteria:**
- [ ] A test with `Capacity = 1` and a blocked handler shows that the second publish waits, and that cancelling its token throws `OperationCanceledException` without enqueuing.
- [ ] A test shows that a waiting publish completes once the consumer frees space.

---

### T7 — Per-handler timeout

**Depends on:** T5.

**Why:** There is a single consumer, so one hung handler stalls every event behind it.

**Changes:**
1. In `EventPublisher`, for each handler invocation (each attempt, once T9 exists), create a `CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)` and call `CancelAfter(options.HandlerTimeout)` unless the timeout is infinite. Pass the linked token to `HandleAsync`.
2. Distinguish the two kinds of cancellation:
   - If the outer token was cancelled (shutdown), rethrow as today.
   - If only the timeout fired, treat it as a handler failure. Log `Handler {Handler} timed out after {Timeout} for event {EventType}`; T9 makes it retryable.
3. Document that handlers must honor the cancellation token, because a handler that ignores it cannot be stopped.

**Acceptance criteria:**
- [ ] A test where a handler awaits `Task.Delay(Timeout.Infinite, ct)` shows the handler is cancelled after `HandlerTimeout`, the next handler and the next event still run, and the timeout is logged.
- [ ] Shutdown cancellation is still rethrown and is not logged as a timeout.

---

### T8 — Graceful shutdown drain

**Depends on:** T2, T6.

**Why:** Today, queued events are dropped and in-flight handlers are cancelled the moment shutdown begins.

**Changes:**
1. In `EventProcessorJob`, add a `CancellationTokenSource abortCts` field and dispose it in `Dispose`.
2. `ExecuteAsync` must ignore its `stoppingToken` for reading. Read with `ReadAllAsync(abortCts.Token)` so the loop runs until the writer is completed **and** the channel is empty. Pass `abortCts.Token` to the dispatch and the handlers.
3. Override `StopAsync`:
   ```csharp
   public override async Task StopAsync(CancellationToken cancellationToken)
   {
       messageQueue.Writer.TryComplete();                                  // stop accepting, start draining
       await using var _ = cancellationToken.Register(abortCts.Cancel);   // hard stop at the host shutdown timeout
       await base.StopAsync(cancellationToken);
   }
   ```
4. The `catch (OperationCanceledException) when (...)` in the loop must check `abortCts.IsCancellationRequested` instead of `stoppingToken`.
5. After abort, log the number of events still in the queue (`Reader.Count`) at Warning as `{DroppedCount} events dropped at shutdown`.
6. **Late publishes.** Once the writer is completed, `WriteAsync` throws `ChannelClosedException`. Let it propagate to the caller, and document the behavior in the XML docs of `IEventBus.PublishAsync`. HTTP requests that are still in flight during shutdown can hit this.
7. Document that the drain window is `HostOptions.ShutdownTimeout` (default 30 s), and that it must fit within the orchestrator's grace period (Kubernetes `terminationGracePeriodSeconds`).

**Acceptance criteria:**
- [ ] A test enqueues N events with a slow handler and stops the host with a generous timeout: all N are handled.
- [ ] A test with a short shutdown timeout shows the handler's token is cancelled at the timeout, and the dropped count is logged.
- [ ] A publish after `StopAsync` has started throws `ChannelClosedException`.

---

### T9 — Handler retries

**Depends on:** T5, T7.

**Why:** Today, a transient failure means the event is permanently lost for that handler.

**Changes:**
1. In `EventPublisher`, wrap each handler invocation in a retry loop. Retry **only the failed handler**, never the whole event, otherwise handlers that already succeeded run twice.
2. Policy: up to `RetryCount` retries after the first attempt, with exponential backoff and jitter. The delay is `min(RetryBaseDelay * 2^(attempt-1), RetryMaxDelay)` multiplied by a random factor in [0.8, 1.2]. Use `TimeProvider` for delays (`Task.Delay(delay, timeProvider, ct)`) so tests can use `FakeTimeProvider`.
3. What gets retried:
   - Retry exceptions thrown by the handler and handler timeouts (T7).
   - Do not retry when the outer (shutdown) token is cancelled. Rethrow.
   - Optionally, let handlers opt out by throwing a library exception such as `NonRetryableEventException`.
4. Each attempt gets a fresh timeout (T7).
5. Log levels: a failed attempt is Warning (`attempt {Attempt}/{MaxAttempts}`). The final failure is Error with `MessageId`, `EventType`, `Handler` and attempt count. In-memory, this log entry is the dead letter, so it must contain enough to identify what was lost.
6. Alternatively, implement with `Microsoft.Extensions.Resilience` / Polly v8 `ResiliencePipeline`. That is acceptable if it doesn't pull in more dependencies than warranted. Prefer the small hand-written loop for a minimal package.
7. README: handlers **must be idempotent**, because they can now run more than once for the same event.

**Acceptance criteria:**
- [ ] A handler that fails twice and then succeeds runs exactly 3 times, and other handlers of the same event run exactly once.
- [ ] A handler that always fails runs `RetryCount + 1` times, then is logged at Error, and processing continues with the next handler and event.
- [ ] Backoff delays are verified with `FakeTimeProvider`.

---

### T10 — Metrics

**Depends on:** T2, T6. T9 is needed for the retry counter.

**Changes:**
1. Create a `Meter` named `Infinity.Toolkit.Handlers` via an injected `IMeterFactory`, so tests can capture it.
2. Instruments (use OTel units: seconds for durations):
   - `infinity.handlers.queue.depth` — `ObservableGauge<int>` reading `Reader.Count`. Check `Reader.CanCount`.
   - `infinity.handlers.queue.latency` (s) — `Histogram<double>`: dequeue time minus `EnqueuedAt`.
   - `infinity.handlers.process.duration` (s) — `Histogram<double>` per event, tag `infinity.event_type`.
   - `infinity.handlers.handler.duration` (s) — `Histogram<double>` per handler, tags `infinity.event_type`, `infinity.handler`, `outcome` (`success` | `failure` | `timeout`).
   - `infinity.handlers.handler.retries` — `Counter<long>`, same tags.
   - `infinity.handlers.events.dropped` — `Counter<long>`, incremented at shutdown (T8).
3. Expose a public constant `InfinityHandlersDiagnostics.MeterName`, and document `.AddMeter(...)` in the README.
4. Keep tag cardinality bounded: event type and handler type names only, never message IDs.

**Acceptance criteria:**
- [ ] Tests using `MetricCollector<T>` (`Microsoft.Extensions.Diagnostics.Testing`) verify each instrument.

---

### T11 — Health check

**Depends on:** T10, or at least the queue-latency measurement from it.

**Changes:**
1. Add an internal singleton `ProcessorState` that `EventProcessorJob` updates: `IsRunning`, `FaultedWith` (Exception?), and `LastQueueLatency`.
2. Set `FaultedWith` if `ExecuteAsync` exits with an unexpected exception. Wrap the whole method body for this.
3. Add a public `InMemoryMessageQueueHealthCheck : IHealthCheck`:
   - **Unhealthy** if the processor has faulted, or has stopped while the host is not stopping.
   - **Degraded** if `LastQueueLatency > options.DegradedQueueLatency`, or the queue is at capacity.
   - **Healthy** otherwise. Include queue depth and latency in the `data` dictionary.
4. Add an extension method `IHealthChecksBuilder.AddInMemoryMessageQueue(string name = "inmemory-message-queue", HealthStatus? failureStatus = null, IEnumerable<string>? tags = null)`.

**Acceptance criteria:**
- [ ] Tests cover all three states.

---

### T12 — Handler registration helpers

**Depends on:** nothing.

**Changes:**
1. Add `AddEventHandler<TEvent, THandler>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped) where TEvent : IEvent where THandler : class, IEventHandler<TEvent>`. Register with `TryAddEnumerable` so a duplicate registration of the same handler is ignored.
2. Optionally, add `AddEventHandlersFromAssembly(Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)`. It scans for non-abstract classes implementing `IEventHandler<>` (including multiple implementations on one class) and registers each closed interface. Mark it `[RequiresUnreferencedCode]`.
3. Update the README and sample app to use the helpers.

**Acceptance criteria:**
- [ ] Registering the same handler twice results in one invocation per event.
- [ ] Assembly scanning finds a class implementing two `IEventHandler<>` interfaces and registers both.

---

### T13 — Multiple consumers (optional, opt-in)

**Depends on:** T5, T8.

**Why:** A single sequential consumer caps throughput. Only implement this if metrics (T10) show queue latency growing under real load.

**Changes:**
1. When `ConsumerCount > 1`, `EventProcessorJob` runs `ConsumerCount` read loops concurrently over the same reader (`Task.WhenAll`), each with its own scope per event. Set `SingleReader = false` (see T6).
2. Shutdown drain (T8) must wait for all loops.
3. README: with `ConsumerCount > 1`, **ordering across events is not guaranteed**. Handlers for a single event still run sequentially.

**Acceptance criteria:**
- [ ] With `ConsumerCount = 4` and handlers that take 100 ms, throughput is roughly 4×.
- [ ] Drain still processes every queued event.

---

### T14 — Test suite and test support

**Depends on:** nothing. Runs alongside all tasks.

**Changes:**
1. Create a test project, for example `Infinity.Toolkit.Handlers.Tests`, with xUnit (or the repository's existing framework), `Microsoft.Extensions.TimeProvider.Testing`, and `Microsoft.Extensions.Diagnostics.Testing`.
2. Add host-based integration test helpers: build a `Host` with the queue and test handlers, plus a way to **wait until idle** (queue empty and no handler in flight). Expose this through an internal `IQueueIdleSignal` made visible to tests with `InternalsVisibleTo`, or through a public test-support package. Avoid fixed `Task.Delay` sleeps in tests.
3. Regression tests for earlier bugs:
   - Dispatch reaches the handler when the event is published with static type `IEvent`.
   - Struct events are dispatched.
   - Two handlers sharing a scoped dependency do not run concurrently.
   - A handler failure does not stop other handlers or the processor.
   - A null event is rejected by `PublishAsync`.

**Acceptance criteria:**
- [ ] The tests run in CI. Every task above has its tests here.

---

### T15 — Packaging and documentation

**Depends on:** all other Part 1 tasks.

**Changes:**
1. Add XML documentation on all public types and members, and enable `GenerateDocumentationFile`.
2. Add `Microsoft.CodeAnalysis.PublicApiAnalyzers` with `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`.
3. Enable SourceLink and deterministic builds, and publish a symbol package (`.snupkg`).
4. Complete the README: quick start, options reference (T5), delivery and ordering guarantees (T1), idempotency requirement (T9), OpenTelemetry setup (T4, T10), health check (T11), shutdown behavior (T8).
5. If the package targets trimming or AOT, mark `EventDispatcher.For` (which uses `MakeGenericType`) and assembly scanning with `[RequiresDynamicCode]` / `[RequiresUnreferencedCode]`, and propagate those annotations to the public entry points.

**Acceptance criteria:**
- [ ] The package builds with no documentation or public API warnings.
- [ ] The README covers every item above.

---

## Part 2 — Guaranteed delivery (deferred)

**Do not start these until the decision is made that losing an event is no longer acceptable.**

### Decision guide

There are two routes, and they can be combined:

- **Transactional outbox with local dispatch (D1–D4).** Events are persisted in the same database transaction as the state change, and a relay dispatches them in-process. You get no new infrastructure. Handlers still run in the same service. This fits when producers and consumers are in the same service.
- **Message broker (D1, D2, D4, D5).** Events go to Azure Service Bus. This is needed when consumers live in other services, or when you need broker features such as dead-letter queues, sessions, or scheduled delivery. To be atomic with database changes, it should still be fed through the outbox (D3 publishing to D5).

Write the decision down as an ADR before starting.

---

### D1 — Transport abstraction and shared pipeline

**Depends on:** Part 1 complete, at least T2–T9.

**Why:** The dispatch pipeline (scope per event, runtime-type dispatch, sequential handlers, timeout, retries, tracing, metrics) must work identically regardless of where the envelope comes from.

**Changes:**
1. Extract the per-envelope processing from `EventProcessorJob` into an internal `EventPipeline` with `Task<PipelineResult> ProcessAsync(EventEnvelope envelope, CancellationToken ct)`. `PipelineResult` reports, per handler, whether it succeeded or failed after retries.
2. `EventProcessorJob` (in-memory) becomes one envelope source among several. Add an internal `IEventTransport` (or equivalent) for producers, so `IEventBus` can route to in-memory, outbox, or broker.
3. Extend `EventEnvelope` with whatever durable transports need, for example an attempt count and the original W3C `traceparent` string. `ActivityContext` is not serializable, so convert it with `ActivityContext.TryParse` and `Activity.Id`.

**Acceptance criteria:**
- [ ] The in-memory behavior and all Part 1 tests are unchanged.
- [ ] The pipeline has unit tests independent of the channel.

---

### D2 — Serialization contract and event type registry

**Depends on:** D1.

**Why:** A persisted or transmitted event must be deserialized back to the right CLR type, possibly by a newer version of the code.

**Changes:**
1. Add an event type registry that maps a **stable** event type name to a CLR type and back. Names must be explicit and must not change when the class is renamed or moved. Provide them via an attribute (`[EventType("order-placed.v1")]`) or registration (`AddEvent<OrderPlaced>("order-placed.v1")`). Fail at startup if a handled event type has no registered name. Reconcile with `IEvent.EventType`: either derive it from the registry, or validate at startup that they match.
2. Centralize `JsonSerializerOptions` in options (default `JsonSerializerDefaults.Web`). Use the same options for `EventHandlerContext.Body`, so `Body` has the same shape in every transport.
3. Document the versioning rules: additive changes only within a version, and a new name (`.v2`) for breaking changes. Readers must tolerate unknown properties.

**Acceptance criteria:**
- [ ] A round-trip test from event to JSON plus type name and back to an equal event passes for class and struct events.
- [ ] Unknown type names during deserialization produce a clear error, and the message is dead-lettered (D3/D5), not lost.

---

### D3 — Transactional outbox (EF Core)

**Depends on:** D1, D2.

**Why:** This publishes events atomically with the state change that caused them. Without it, a crash between `SaveChanges` and publish loses the event, or a publish before a failed `SaveChanges` emits an event for a change that never happened.

**Changes:**
1. Create a new package, for example `Infinity.Toolkit.Handlers.Outbox.EntityFrameworkCore`.
2. Add an `OutboxMessage` entity with these columns: `Id` (= MessageId), `EventType`, `Payload` (JSON), `CreatedAt`, `TraceParent`, `Attempts`, `LastError` (truncated, no payload), `LockedUntil`, `LockedBy`, `ProcessedAt`, `DeadLetteredAt`. Index `(ProcessedAt, DeadLetteredAt, CreatedAt)`.
3. Add a `ModelBuilder.AddOutbox()` extension so it can be included in the application's own `DbContext` and migrations.
4. **Producer API.** Add an `IOutbox` (scoped) with `void Enqueue(IEvent @event)`. It adds an `OutboxMessage` to the current `DbContext` and does **not** save. The caller's `SaveChanges` / transaction commits the state change and the event together. Document that the event is only published if the caller saves.
5. **Relay.** Add a `BackgroundService` that:
   - Claims a batch of unprocessed, non-dead-lettered rows whose lock has expired, using a conditional update that sets `LockedUntil` / `LockedBy`, so multiple replicas don't process the same row. For SQL Server, use `UPDATE TOP (@batch) ... WITH (UPDLOCK, READPAST, ROWLOCK) ... OUTPUT inserted.*`, or an equivalent claim with optimistic concurrency.
   - Deserializes each row (D2), restores trace context, and hands it to the pipeline (D1) for local dispatch, or to the broker (D5).
   - On success, sets `ProcessedAt`. On failure, increments `Attempts`, sets `LastError`, and releases the lock. After `MaxAttempts`, sets `DeadLetteredAt`.
   - Polls every `PollingInterval` (default 1–5 s). Optionally, use an in-process signal raised after `SaveChanges` (via a `SaveChangesInterceptor` that detects new outbox rows) to wake the relay immediately and reduce latency.
6. **Retention.** Add a periodic cleanup that deletes processed rows older than `RetentionPeriod`. Dead-lettered rows are kept until handled (D6).
7. **Delivery semantics.** This is **at-least-once**: a crash after handling but before `ProcessedAt` is written causes redelivery. D4 is therefore required.

**Acceptance criteria:**
- [ ] Integration tests against a real database (Testcontainers) verify the following:
  - If `SaveChanges` rolls back, no event is processed.
  - If the process is killed after commit, the event is processed after restart.
  - Two relay instances never process the same row concurrently.
  - A poison message is dead-lettered after `MaxAttempts`.

---

### D4 — Idempotent consumers (inbox)

**Depends on:** D1. Required by D3 and D5.

**Why:** At-least-once delivery means a handler can see the same `MessageId` more than once. With retry granularity per event (D3/D5), handlers that already succeeded may be re-invoked when a sibling handler fails.

**Changes:**
1. Add an `InboxRecord` entity with `(MessageId, Handler)` as the primary key, plus `ProcessedAt`.
2. Add a pipeline step: before invoking a handler, skip it if `(MessageId, Handler)` exists. After the handler succeeds, insert the record.
3. **For exactly-once effects** on handlers that write to the same database, the inbox insert must commit in the same transaction as the handler's changes. Provide an opt-in for EF Core handlers that share the scoped `DbContext`: the pipeline opens a transaction, runs the handler, adds the inbox record, and commits. Document that handlers with external side effects (HTTP calls, email) cannot be made exactly-once this way and must be idempotent themselves.
4. Add retention cleanup, keeping records at least as long as the maximum redelivery window.
5. Expose `MessageId` on the handler context (see T2 step 5) if that isn't done yet.

**Acceptance criteria:**
- [ ] Delivering the same envelope twice invokes each handler once.
- [ ] If handler B fails and the message is redelivered, handler A (already succeeded) is not re-invoked.
- [ ] With the transactional opt-in, a crash between the handler's writes and the inbox insert leaves neither committed.

---

### D5 — Azure Service Bus transport

**Depends on:** D1, D2, D4. Should be fed by D3 when atomicity with database writes is needed.

**First, check whether Infinity.Toolkit already has a Service Bus messaging package** (the existing `IHandlerContext` / `Body` shape suggests one may exist). If it does, integrate with it rather than building a new one.

**Changes:**
1. **Publishing.** Use `ServiceBusSender` (`Azure.Messaging.ServiceBus`). Body = serialized event (D2). Set `MessageId` = envelope `MessageId`, so the entity's duplicate detection can be used. Set `Subject` / `ApplicationProperties["EventType"]` = registered event type name, and set the trace context. The SDK propagates `Diagnostic-Id` / `traceparent`; verify how this works with the current SDK version, including whether its ActivitySource tracing must be enabled via the `Azure.Experimental.EnableActivitySource` switch.
2. **Topology.** Use one topic, or a topic per bounded context, with one subscription per consuming service. Filter on `EventType` via SQL or correlation filters. Make this configurable, and document what infrastructure (Bicep) it expects.
3. **Consuming.** Use `ServiceBusProcessor`. Map each received message to an envelope and run the pipeline (D1).
   - Retry granularity is **per message**. If any handler still fails after in-process retries, abandon the message. Service Bus redelivers until `MaxDeliveryCount`, then moves it to the dead-letter queue. D4 prevents succeeded handlers from re-running.
   - Complete the message only after all handlers succeed (or are skipped by D4).
   - Set `MaxConcurrentCalls` from options. If ordering is required, use sessions with a session ID derived from the event (for example the aggregate ID), and document it.
4. **Authentication.** Use `DefaultAzureCredential` / managed identity, not connection strings. Make the fully qualified namespace configurable.
5. **Health check.** Report sender/processor state and processor errors.

**Acceptance criteria:**
- [ ] Integration tests against the Service Bus emulator or a test namespace verify these cases:
  - A published event is handled by a subscriber.
  - A failing handler leads to redelivery and then the dead-letter queue.
  - A duplicate `MessageId` is not processed twice.
  - A trace links the producer and the consumer.

---

### D6 — Dead-letter inspection and replay

**Depends on:** D3 and/or D5.

**Changes:**
1. **Outbox.** Add an API (`IOutboxAdmin`) to list dead-lettered rows (metadata only: id, type, attempts, last error, timestamps; no payload by default), replay a row (reset `DeadLetteredAt`, `Attempts`, and the lock), and discard it.
2. **Service Bus.** Document the use of the DLQ sub-queue. Optionally, add a resubmit helper that copies a DLQ message back to the topic with the same `MessageId`.
3. Expose these through an opt-in, authorized admin endpoint or a small CLI. Never expose them by default.
4. Add metrics for dead-lettered counts per event type (extends T10), and an alert recommendation in the README.

**Acceptance criteria:**
- [ ] A replayed outbox message is processed exactly once (D4).
- [ ] The admin surface requires authorization.

---

### D7 — Per-event-type routing

**Depends on:** D1, plus D3 and/or D5.

**Why:** Not every event needs guaranteed delivery. Routing lets you keep cheap in-memory dispatch for best-effort events and use outbox or broker delivery only where needed.

**Changes:**
1. Add a routing configuration in options, for example:
   ```csharp
   services.AddInfinityHandlers(o =>
   {
       o.Route<CacheInvalidated>().InMemory();      // default
       o.Route<OrderPlaced>().ViaOutbox();          // D3, local dispatch
       o.Route<InvoiceIssued>().ViaOutbox().ToServiceBus(); // D3 → D5
   });
   ```
2. `IEventBus.PublishAsync` routes by the event's runtime type. For outbox routes, `PublishAsync` must be called within a unit of work whose `SaveChanges` commits the event. Decide whether to keep `IEventBus` for both cases, or require `IOutbox` explicitly for durable routes. Making durable publishing explicit at the call site is safer, because an `IEventBus.PublishAsync` that silently depends on a later `SaveChanges` is easy to misuse.
3. Validate at startup that every route's transport is registered.

**Acceptance criteria:**
- [ ] Tests verify that each route reaches its transport.
- [ ] Misconfiguration fails at startup.
