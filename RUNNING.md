# 🧪 Manual Testing Notes

* 🔐 JWT works for both services; `AuthController` exists in InventoryService for simplicity.
* 🐳 Start everything: `docker compose up --build`
* 🌐 Swagger:

  * Product → [http://localhost:5001/swagger](http://localhost:5001/swagger)
  * Inventory → [http://localhost:5002/swagger](http://localhost:5002/swagger)
* 📘 Swagger examples are implemented for all main endpoints.
* 🔑 Generate token: `POST /auth/token`, then Authorize in Swagger for both services.
* 🛍 Create product: `POST /products` → copy returned `Id`.
* 📦 Add inventory: `POST /inventory` with above `ProductId`
* 🔄 Verify amount updated: `GET /products`
* 📊 Logs: [http://localhost:3000](http://localhost:3000) → Grafana → Drilldown → Logs.


# Running All Tests

* run `dotnet test` in the main directory (with **slnx** file)

# ⚠️ TODOs: 

* authenticate Product HTTP client (Inventory→Product) 
* add minimal tracing/spans (DB, HTTP, publish, consume, store) visible in Grafana.
