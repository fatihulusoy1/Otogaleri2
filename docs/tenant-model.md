# Tenant Model

## Multi-Tenancy Strategy
We use a **Shared Database, Shared Schema** approach.

- Every business entity implements `ITenantEntity`.
- `TenantId` is used to filter records globally.
- Security is enforced via EF Core Global Query Filters.

## Row-Level Security (RLS)
PostgreSQL RLS can be enabled as an additional layer of security:
```sql
ALTER TABLE Vehicles ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON Vehicles
USING (tenant_id = current_setting('app.current_tenant')::uuid);
```
*Note: Current implementation uses EF Core filters.*
