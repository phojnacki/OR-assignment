# 📐 Assumptions & Architectural Decisions

Overview of key assumptions behind the implementation.

---

## 🔄 Self-Healing & Fault Tolerance

* 🧩 **Any of the 5 components can be turned off** (ProductService, InventoryService, DBs, RabbitMQ).
* ♻️ The system is **self-healing** - once a component is restored, processing resumes automatically.
* 🚫 **No data loss. No event loss.**
* 📨 Messages are persisted and retried safely thanks to durable messaging patterns.

---

## 📦 Reliable Messaging (Wolverine Built-In Patterns)

* 🗃️ **Transactional Outbox Pattern**

  * Inventory entry is stored **and** event is persisted to the outbox in a **single database transaction**.
  * Guarantees atomicity: if DB commit succeeds → event is guaranteed to be published.

* 📥 **Transactional Inbox Pattern**

  * Incoming event is:

    * Stored in Wolverine internal inbox table.
    * Acknowledged.
    * Product update is executed.
  * All performed inside **one DB transaction**.

This ensures:

* Exactly-once processing semantics (effectively).
* Safe retries.

---

## 🆔 Idempotency Strategy

* `ProductInventoryAddedEvent` contains a **business identifier (`EventId`)**.
* ProductService maintains a `ProcessedEvents` collection/table.
* Handler checks if `EventId` already exists → if yes, skip processing.

✔ Guarantees:

* Re-delivered messages do not duplicate updates.
* Safe in at-least-once delivery environments.

### 💡 Alternative Considered

* Map `EventId` to RabbitMQ `MessageId` via Wolverine mapping.
* Would require custom header mapping and internal message configuration.
* Less elegant and tightly coupled to transport details.
* Available upon request but not chosen for this assignment.

---

## 🧹 ProcessedEvents Cleanup

* 🧵 Implemented as a background task.
* Old processed event entries are pruned safely after retention period.
* (*) May be implemented using e.g. **Hangfire** or scheduled hosted service

---

## 🔎 Product Existence Validation Strategy

* 🆕 ProductService publishes `ProductCreatedEvent`.
* InventoryService maintains a local `KnownProducts` table.

When adding inventory:

1. If ProductId exists in `KnownProducts` → proceed.
2. If not:

   * ⚠️ Rare scenario (event not yet delivered or truly non-existent product).
   * InventoryService performs an **HTTP call** to ProductService.
   * If product exists → cache locally.
   * If not → reject request.

✔ Ensures:

* No silent event drops. We know that product does not exists right away on POST /inventory call.
* No inconsistent inventory writes.
* Eventual consistency without sacrificing correctness.

---

## 📊 Observability & Logging

* 📈 **Grafana** added for log aggregation & visualization.
* Structured logging implemented across services.
* Errors are:

  * Structured
  * Traceable

---

## 🏗 Clean Architecture Principles

System follows strict layered separation:

### 🎯 API Layer

* Inbound concerns
* Authentication (JWT)
* Authorization (RBAC: `read` / `write`)
* Validation

### 🧠 Application Layer

* Use cases
* Command/Query handlers

### 🧬 Domain Layer

* Entities
* Domain rules, pure methods (e.g. Increase amount)

### 🔌 Infrastructure Layer

* Database implementations
* HTTP clients
* Backround jobs

✔ Inbound dependencies implemented in API (presentation) layer
✔ Outbound dependencies implemented in Infrastructure
✔ Domain remains framework-agnostic

---

## 🔐 Security Assumptions

* All endpoints protected by **JWT authentication**.
* Role-based access control:

  * `write` → POST operations
  * `read` → GET operations
* Services validate tokens independently.
* No anonymous access allowed.

---

## 🧪 Testing Philosophy

* 🧪 Integration tests verify:

  * Event publication
  * Idempotent consumption
* 🧪 End-to-end tests use:

  * TestContainers
  * Full environment spin-up (DB + RabbitMQ + both services)

System behavior tested under:

* Duplicate message delivery
* Component restarts
* Delayed event processing

---

## 🚀 Design Goal Summary

The system is designed to be:

* ✅ Resilient
* ✅ Idempotent
* ✅ Eventually consistent
* ✅ Transport-agnostic
* ✅ Cleanly layered
* ✅ Observable (logs in Grafana)
* ✅ Docker-ready
