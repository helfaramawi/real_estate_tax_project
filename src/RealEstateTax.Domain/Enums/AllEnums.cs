namespace RealEstateTax.Domain.Enums;

public enum AppealStatus
{
    Submitted = 0,
    UnderReview = 1,
    Assigned = 2,
    HearingScheduled = 3,
    Approved = 4,
    PartiallyApproved = 5,
    Rejected = 6,
    Withdrawn = 7
}

public enum BillStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5,
    Disputed = 6
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3,
    Cancelled = 4
}

public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    OnlinePortal = 2,
    MobileWallet = 3,
    PostOffice = 4,
    BankCard = 5
}

public enum ValuationStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Superseded = 4
}

public enum ValuationMethod
{
    MarketComparison = 0,
    IncomeCapitalization = 1,
    CostApproach = 2,
    RentalValue = 3,    // Standard for Egyptian property tax
    MassAppraisal = 4
}

public enum ExemptionStatus
{
    Submitted = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
    Revoked = 5
}

public enum ExemptionType
{
    Agricultural = 0,
    Religious = 1,
    Diplomatic = 2,
    GovernmentOwned = 3,
    LowIncome = 4,
    Disabled = 5,
    WarVeteran = 6,
    CharitableOrganization = 7,
    NewConstruction = 8,    // TODO: verify Egyptian law grace period
    Other = 99
}

public enum SurveyStatus
{
    Assigned = 0,
    InProgress = 1,
    Submitted = 2,
    Reviewed = 3,
    Approved = 4,
    Rejected = 5
}

public enum RiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum FraudFlagType
{
    DuplicateRegistration = 0,
    SuspiciousOwnershipTransfer = 1,
    UndervaluationSuspected = 2,
    MissingDocuments = 3,
    AddressMismatch = 4,
    NationalIdMismatch = 5,
    InternalTip = 6,
    ExternalSystemAlert = 7
}

public enum FraudFlagStatus
{
    Open = 0,
    UnderInvestigation = 1,
    Confirmed = 2,
    Dismissed = 3
}

public enum IntegrationRequestStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    RetryScheduled = 4
}

public enum IntegrationDirection
{
    Inbound = 0,
    Outbound = 1
}

public enum NotificationType
{
    BillIssued = 0,
    PaymentReceived = 1,
    AppealSubmitted = 2,
    AppealDecision = 3,
    ExemptionApproved = 4,
    FieldSurveyAssigned = 5,
    ValuationCompleted = 6,
    RiskFlagRaised = 7,
    SystemAlert = 8,
    BillReminder = 9,
    OverdueNotice = 10
}

public enum NotificationChannel
{
    Email = 0,
    SMS = 1,
    Portal = 2,
    Push = 3
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Failed = 3,
    Read = 4
}

public enum PropertyType
{
    Residential = 0,
    Commercial = 1,
    Industrial = 2,
    Agricultural = 3,
    Mixed = 4,
    Vacant = 5,
    Institutional = 6
}

public enum OwnershipType
{
    Sole = 0,
    Joint = 1,
    Corporate = 2,
    Inherited = 3,
    Disputed = 4
}

public enum SourceSystem
{
    ManualEntry = 0,
    CadastralRegistry = 1,
    ElectricityUtility = 2,
    WaterUtility = 3,
    TelecomRegistry = 4,
    MunicipalRecords = 5,
    RealEstateRegistry = 6,
    FieldSurvey = 7,
    ExternalIntegration = 8
}

public enum DataQualityIssueType
{
    MissingCoordinates = 0,
    DuplicateSuspected = 1,
    InvalidNationalId = 2,
    MissingOwnerInfo = 3,
    AreaDiscrepancy = 4,
    InvalidAddress = 5,
    MissingPropertyType = 6,
    ConflictingSourceData = 7
}

public enum DataQualityIssueSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

public enum DataQualityIssueStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Dismissed = 3
}

public enum AssessmentStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Objected = 3,
    Revised = 4,
    Final = 5
}
