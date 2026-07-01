# Object Storage Abstraction

## Purpose

The TOEIC platform stores PDFs, audio, images, transcripts, extracted source files, and generated media outside the relational database.

P1.4 creates the storage port and a local test double. Provider-specific cloud storage is added later.

## Application Contract

Application-owned interface:

```text
Application.Common.Interfaces.Storage.IObjectStorage
```

Operations:

- `Put(PutObjectRequest request)`
- `Get(ObjectKey key)`
- `List(string prefix)`
- `Delete(ObjectKey key)`

Value objects:

- `ObjectKey`
- `PutObjectRequest`
- `StoredObject`

## Infrastructure Implementation

Local/test implementation:

```text
Infrastructure.Storage.InMemoryObjectStorage
```

Registered by:

```text
Infrastructure.DependencyInjection.AddInfrastructureDependencies
```

## Business Rules

1. Source/media bytes must not be stored directly in normalized TOEIC domain tables.
2. Database records store metadata and object keys.
3. Object keys must be explicit and non-empty.
4. Content type must be explicit and non-empty.
5. Storage implementation must copy bytes on write/read to avoid accidental mutation.

## Out Of Scope

- S3/R2/Azure provider implementation
- signed URLs
- CDN
- virus scanning
- media transcoding
- large multipart upload

## Failure Modes To Handle Later

- object not found
- invalid key
- provider outage
- timeout
- unauthorized storage credential
- partial upload
- checksum mismatch

P1.4 covers object-not-found and local test double behavior. Provider failures are covered when cloud provider implementation is introduced.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "IObjectStorage|InMemoryObjectStorage|Object Storage Abstraction" backend/src backend/tests docs/product
```
