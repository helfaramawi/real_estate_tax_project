export interface ApiResponse<T> {
  success: boolean
  data: T
  error?: string
  errors?: string[]
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface TokenResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  roles: string[]
  permissions: string[]
  username: string
}

export interface Property {
  id: string
  propertyCode: string
  type: number
  typeName: string
  status: number
  statusName: string
  builtUpArea: number
  landArea?: number
  yearBuilt?: number
  streetAddress?: string
  district?: string
  city: string
  governorate: string
  fullAddress?: string
  currentAssessedValue?: number
  currentTaxAmount?: number
  createdAt: string
  updatedAt?: string
}

export interface Taxpayer {
  id: string
  taxpayerCode: string
  nationalId: string
  firstName?: string
  lastName?: string
  companyName?: string
  isCorporate: boolean
  email?: string
  phoneNumber?: string
  governorate?: string
  city?: string
  createdAt: string
}

export interface TaxBill {
  id: string
  billNumber: string
  propertyId: string
  propertyCode?: string
  taxYear: number
  totalAmount: number
  paidAmount: number
  remainingAmount: number
  status: number
  statusName: string
  dueDate: string
  issueDate?: string
  periodStart: string
  periodEnd: string
}

export interface Payment {
  id: string
  billId: string
  billNumber?: string
  amount: number
  method: number
  methodName: string
  reference?: string
  paidAt: string
}

export interface Appeal {
  id: string
  appealNumber: string
  propertyId: string
  taxpayerId: string
  reason: string
  status: number
  statusName: string
  submittedAt: string
  decisionAt?: string
  decisionNotes?: string
}

export interface Exemption {
  id: string
  propertyId: string
  type: number
  typeName: string
  status: number
  statusName: string
  exemptionPercentage?: number
  exemptAmount?: number
  startDate?: string
  endDate?: string
}

export interface Valuation {
  id: string
  propertyId: string
  method: number
  methodName: string
  grossValue: number
  deductionRate: number
  netValue: number
  taxYear: number
  status: number
  statusName: string
  valuedAt: string
}

export interface DashboardKpis {
  totalProperties: number
  totalTaxpayers: number
  totalBillsIssued: number
  totalCollected: number
  totalOverdue: number
  pendingAppeals: number
  pendingExemptions: number
  highRiskProperties: number
}
