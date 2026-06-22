# Paraki API Contract

Endpoints available for frontend consumption. Base URL: `http://localhost:5000/api` (dev) or production equivalent.

All endpoints require JWT token in `Authorization: Bearer <token>` header unless marked as `[AllowAnonymous]`.

## Authentication

### Login
```
POST /api/auth/login
Body: { email: string, password: string }
Response 200: { token: string, usuario: UserInfo }
Response 400: { error: true, message: string }
```

### Register
```
POST /api/auth/register
Body: { displayName: string, email: string, password: string, type: int }
Response 200: { token: string, usuario: UserInfo }
Response 400: { error: true, message: string }
```

## Users (Usuarios)

All endpoints require authentication.

### Get Current User Profile
```
GET /api/usuarios/me
Response 200: {
  id: string (guid),
  name: string,
  email: string,
  userType: int (0=User, 1=Operator, 2=Admin),
  createdAt: datetime,
  fotoPerfilUrl: string (nullable)
}
```

### Get Current User's Ratings
```
GET /api/usuarios/me/avaliacoes
Response 200: [
  {
    id: string,
    bicicletarioId: string,
    usuarioId: string,
    rating: int (1-5),
    comment: string (nullable),
    createdAt: datetime
  }
]
```

### Get Current User's Bike Racks (claimed/managed)
```
GET /api/usuarios/me/bicicletarios
Response 200: [
  {
    id: string,
    name: string,
    latitude: decimal,
    longitude: decimal,
    hasPowerOutlet: bool,
    hasAirPump: bool,
    hasLocker: bool,
    hasStorage: bool,
    hasMaintenanceSpace: bool,
    hasBikeLock: bool,
    isFree: bool,
    isPaid: bool,
    requiresSignup: bool,
    isMonthlySubscription: bool,
    vehicleTypes: int (flags),
    operatorId: string,
    ratings: [...],
    createdAt: datetime,
    updatedAt: datetime,
    isDeleted: bool
  }
]
```

### Upload Profile Photo
```
POST /api/usuarios/me/foto
Content-Type: multipart/form-data
Form Data: foto (file) — max 5MB, types: webp, png, jpeg
Response 200: { url: string (relative path) }
Response 400: { error: true, message: string }
```

## Bike Racks (Bicicletarios)

### List All Bike Racks (Nearby/Filtered)
```
GET /api/bicicletarios?latitude={lat}&longitude={lng}&radiusKm={radius}&skip={n}&take={n}
Query Params (all optional):
  - latitude, longitude: decimal — center point for distance filter
  - radiusKm: int — search radius (default all)
  - skip: int — pagination offset (default 0)
  - take: int — page size (default 20)
  
Response 200: [
  {
    id: string,
    name: string,
    latitude: decimal,
    longitude: decimal,
    /* service flags, access flags, vehicle types as above */
    operatorId: string,
    ratings: [{ rating, comment, usuarioId, createdAt }, ...],
    createdAt: datetime,
    updatedAt: datetime,
    isDeleted: bool
  }
]
```

### Get Bike Rack Details
```
GET /api/bicicletarios/{id}
Response 200: { /* same structure as list */ }
Response 404: { error: true, message: "Not found" }
```

### Create Bike Rack
```
POST /api/bicicletarios
Body: {
  name: string,
  latitude: decimal,
  longitude: decimal,
  hasPowerOutlet: bool,
  hasAirPump: bool,
  hasLocker: bool,
  hasStorage: bool,
  hasMaintenanceSpace: bool,
  hasBikeLock: bool,
  isFree: bool,
  isPaid: bool,
  requiresSignup: bool,
  isMonthlySubscription: bool,
  vehicleTypes: int (flags)
}
Response 201: { id: string, /* full object */ }
Response 400: { error: true, message: string }
```

### Update Bike Rack
```
PUT /api/bicicletarios/{id}
Body: { /* same as Create */ }
Response 200: { /* updated object */ }
Response 404: { error: true, message: "Not found" }
```

### Delete Bike Rack
```
DELETE /api/bicicletarios/{id}
Response 204: (no content)
Response 404: { error: true, message: "Not found" }
```

## Ratings (Avaliacoes)

### List Ratings for a Bike Rack
```
GET /api/avaliacoes?bicicletarioId={id}&skip={n}&take={n}
Query Params:
  - bicicletarioId: string (guid) — required
  - skip: int — pagination offset (default 0)
  - take: int — page size (default 10)

Response 200: [
  {
    id: string,
    bicicletarioId: string,
    usuarioId: string,
    rating: int (1-5),
    comment: string (nullable),
    createdAt: datetime
  }
]
```

### Create Rating
```
POST /api/avaliacoes
Body: {
  bicicletarioId: string (guid),
  rating: int (1-5),
  comment: string (nullable)
}
Response 201: { id: string, /* full object */ }
Response 400: { error: true, message: string }
```

### Update Rating
```
PUT /api/avaliacoes/{id}
Body: {
  rating: int (1-5),
  comment: string (nullable)
}
Response 200: { /* updated object */ }
Response 404: { error: true, message: "Not found" }
```

### Delete Rating
```
DELETE /api/avaliacoes/{id}
Response 204: (no content)
Response 404: { error: true, message: "Not found" }
```

## Photos (Fotos)

### List Photos for a Bike Rack
```
GET /api/fotos?bicicletarioId={id}&skip={n}&take={n}
Query Params:
  - bicicletarioId: string (guid) — required
  - skip: int — offset (default 0)
  - take: int — page size (default 10)

Response 200: [
  {
    id: string,
    bicicletarioId: string,
    usuarioId: string,
    fotoUrl: string,
    createdAt: datetime
  }
]
```

### Upload Photo for Bike Rack
```
POST /api/fotos?bicicletarioId={id}
Content-Type: multipart/form-data
Query Param: bicicletarioId (guid)
Form Data: foto (file) — max 5MB, types: webp, png, jpeg

Response 200: { url: string }
Response 400: { error: true, message: string }
```

## Error Response Format

All error responses follow this format:
```json
{
  "error": true,
  "message": "Human-readable error message"
}
```

HTTP Status Codes:
- `200/201` — Success
- `204` — Success (no content)
- `400` — Bad request (validation error)
- `401` — Unauthorized (missing/invalid token)
- `403` — Forbidden (insufficient permissions)
- `404` — Not found
- `500` — Server error

## Authentication Tokens

**Token Type:** JWT (HS256)

**Claims:**
- `sub` — User ID (guid)
- `email` — User email
- `iat` — Issued at (unix timestamp)
- `exp` — Expires at (unix timestamp)

**Token Lifetime:** Configured in `appsettings.json`, typically 24-48 hours

**Headers Required:**
```
Authorization: Bearer eyJhbGc...
Content-Type: application/json (for POST/PUT)
```

## Rate Limiting

None currently implemented. Development environment has no rate limits.

## Pagination

Endpoints that return lists support `skip` and `take` query parameters:
- `skip=0&take=20` — First 20 items
- `skip=20&take=20` — Items 21-40

## Notes

- All timestamps are in UTC, ISO 8601 format
- GUIDs are returned as strings
- VehicleTypes is an integer bitmask (0=Bike, 1=Scooter, 2=Monocycle, 4=ESkate)
- Service flags and access flags are booleans
- Photos and ratings are included in bike rack details automatically
- Soft deletes: bike racks with `isDeleted=true` may still be returned but marked
