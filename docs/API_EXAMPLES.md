# API Request Examples

Base URL: `https://api.retax.gov.eg` (or `http://localhost:8080` for local dev)

## Authentication

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "superadmin",
  "password": "Admin@12345"
}
```
**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123...",
    "expiresAt": "2026-04-28T10:00:00Z",
    "roles": ["SuperAdmin"],
    "permissions": ["properties:read", "properties:create", ...]
  }
}
```

### Refresh Token
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "accessToken": "eyJ...",
  "refreshToken": "abc123..."
}
```

---

## Taxpayers

### Create Taxpayer
```http
POST /api/taxpayers
Authorization: Bearer {token}
Content-Type: application/json

{
  "nationalId": "29901011234567",
  "firstName": "Ahmed",
  "lastName": "Hassan",
  "fatherName": "Mohamed",
  "email": "ahmed.hassan@example.com",
  "phoneNumber": "01012345678",
  "address": "12 شارع التحرير، الدقي",
  "governorate": "Giza",
  "city": "Giza",
  "isCorporate": false
}
```

### Search Taxpayers
```http
GET /api/taxpayers?searchTerm=Hassan&page=1&pageSize=20
Authorization: Bearer {token}
```

---

## Properties

### Create Property
```http
POST /api/properties
Authorization: Bearer {token}
Content-Type: application/json

{
  "type": 0,
  "builtUpArea": 120.5,
  "landArea": 150.0,
  "yearBuilt": 1995,
  "streetAddress": "شارع جامعة الدول العربية",
  "buildingNumber": "45",
  "neighbourhood": "المهندسين",
  "district": "الدقي",
  "city": "Giza",
  "governorate": "Giza",
  "latitude": 30.0444,
  "longitude": 31.2357,
  "usageDescription": "شقة سكنية"
}
```

### Find Nearby Properties (GIS)
```http
GET /api/properties/nearby?lat=30.0444&lng=31.2357&radius=300
Authorization: Bearer {token}
```

### Link Owner to Property
```http
POST /api/properties/{propertyId}/link-owner
Authorization: Bearer {token}
Content-Type: application/json

{
  "taxpayerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "ownershipType": 0,
  "ownershipPercentage": 100,
  "ownershipStartDate": "2010-05-15T00:00:00Z",
  "titleDeedNumber": "TD-2010-00123",
  "titleDeedDate": "2010-05-15T00:00:00Z",
  "registrationAuthority": "Giza Real Estate Registry"
}
```

### Verify Property (Maker-Checker)
```http
POST /api/properties/{propertyId}/verify
Authorization: Bearer {token}  (must be different from creator)
Content-Type: application/json

{
  "verificationNotes": "Physical inspection confirmed. Documents verified."
}
```

---

## Enumeration

### Import Source Records from Electricity Utility
```http
POST /api/enumeration/import-source-records
Authorization: Bearer {token}
Content-Type: application/json

{
  "sourceId": "{electricity-source-id}",
  "batchId": "BATCH-2026-001",
  "records": [
    {
      "sourceReferenceId": "ELEC-00123456",
      "ownerName": "Ahmed Hassan",
      "ownerNationalId": "29901011234567",
      "address": "45 شارع جامعة الدول العربية، المهندسين",
      "meterNumber": "MTR-0123456",
      "latitude": 30.0444,
      "longitude": 31.2357,
      "area": 120.5,
      "propertyType": "Residential"
    }
  ]
}
```

### Match Source Records
```http
POST /api/enumeration/match
Authorization: Bearer {token}
Content-Type: application/json

{
  "sourceRecordIds": ["uuid-1", "uuid-2"],
  "minConfidenceThreshold": 0.75,
  "createNewIfNoMatch": false
}
```

---

## Valuation

### Create Valuation
```http
POST /api/valuations
Authorization: Bearer {token}
Content-Type: application/json

{
  "propertyId": "{property-id}",
  "method": 3,
  "taxYear": 2026,
  "valuationDate": "2026-04-28T00:00:00Z",
  "totalArea": 120.5,
  "annualRentalValue": 36000
}
```

### Approve Valuation (must be different officer from preparer)
```http
POST /api/valuations/{id}/approve
Authorization: Bearer {approver-token}
Content-Type: application/json

{
  "approvalNotes": "Values verified against market data. Approved."
}
```

---

## Tax Assessment

### Generate Tax Assessment
```http
POST /api/tax-assessments/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "propertyId": "{property-id}",
  "valuationId": "{approved-valuation-id}",
  "taxYear": 2026
}
```

---

## Bills

### Generate Bill
```http
POST /api/bills/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "taxAssessmentId": "{approved-assessment-id}",
  "dueDate": "2026-06-30T00:00:00Z",
  "allowsInstallments": false
}
```

### Issue Bill (triggers notification to taxpayer)
```http
POST /api/bills/{billId}/issue
Authorization: Bearer {token}
Content-Type: application/json

{}
```

---

## Payments

### Register Payment
```http
POST /api/payments
Authorization: Bearer {token}
Content-Type: application/json

{
  "taxBillId": "{bill-id}",
  "method": 1,
  "amount": 3600.00,
  "externalTransactionId": "BANK-TXN-20260428-001",
  "paidAt": "2026-04-28T09:30:00Z"
}
```

---

## Appeals

### Submit Appeal
```http
POST /api/appeals
Authorization: Bearer {token}
Content-Type: application/json

{
  "propertyId": "{property-id}",
  "taxpayerId": "{taxpayer-id}",
  "taxAssessmentId": "{assessment-id}",
  "groundsSummary": "Assessed rental value exceeds actual market rent by 40%",
  "requestedAssessmentValue": 21600,
  "legalBasis": "Real Estate Tax Law 196/2008, Article X"
}
```

### Record Decision
```http
POST /api/appeals/{appealId}/decision
Authorization: Bearer {token}
Content-Type: application/json

{
  "decision": 4,
  "decisionNotes": "Appeal upheld. Rental value adjusted based on submitted evidence.",
  "revisedAssessmentValue": 24000
}
```

---

## Risk & Fraud

### Recalculate Risk Score
```http
POST /api/risk/recalculate/{propertyId}
Authorization: Bearer {token}
```

### Create Fraud Flag
```http
POST /api/fraud-flags
Authorization: Bearer {token}
Content-Type: application/json

{
  "propertyId": "{property-id}",
  "flagType": 2,
  "severity": 2,
  "description": "Property valuation is 60% below comparable properties in same district",
  "evidence": "{\"comparables\": [{\"code\": \"PROP-2024-001\", \"value\": 60000}]}"
}
```

---

## Dashboard
```http
GET /api/dashboard/kpis
Authorization: Bearer {admin-token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalProperties": 15420,
    "propertiesVerified": 12100,
    "propertiesTaxable": 11800,
    "totalBilledThisYear": 18500000.00,
    "totalCollectedThisYear": 14200000.00,
    "collectionRate": 76.76,
    "pendingAppeals": 234,
    "openFraudFlags": 12,
    "highRiskProperties": 48,
    "asOf": "2026-04-28T10:00:00Z"
  }
}
```
