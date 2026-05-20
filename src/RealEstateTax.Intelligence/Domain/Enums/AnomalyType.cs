namespace RealEstateTax.Intelligence.Domain.Enums;

public enum AnomalyType
{
    UnregisteredBuilding = 0,
    ValuationOutlier = 1,
    SuspiciousOwnership = 2,
    BoundaryOverlap = 3,
    GhostProperty = 4,
    DuplicateCoordinates = 5,
    SurveyInconsistency = 6,
}
