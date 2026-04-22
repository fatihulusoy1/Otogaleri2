# Database Design

## PostgreSQL Tables

### SaaS
- **Tenants:** Name, Identifier, PlanId, Status.
- **SubscriptionPlans:** Name, Price, Limits (MaxUsers, MaxVehicles).

### Identity
- **Users:** Email, PasswordHash, TenantId.
- **Roles:** Name, TenantId.
- **Permissions:** Name, Code.

### Business
- **Vehicles:** Plate, Brand, Model, Year, Price, Status, TenantId.
- **Transactions:** Type (Income/Expense), Amount, Date, TenantId.
- **Customers/Suppliers:** Contact info, TenantId.

## Indexing
- Unique index on `Users.Email`.
- Unique index on `Tenants.Identifier`.
- Indexes on `TenantId` for all business tables to optimize query filters.
