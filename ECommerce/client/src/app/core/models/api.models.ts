// ECommerce.WebAPI sözleşmeleri (System.Text.Json -> camelCase).
// Tarihler UTC ISO-8601 string olarak gelir.

// ---------- Auth ----------
export interface LoginRequest {
  email: string;
  password: string;
}

export type RegisterRequest = LoginRequest;

// Backend'de [JsonConverter(typeof(JsonStringEnumConverter<MultiFactorStatus>))] -> string.
export type MultiFactorStatus = 'SetupRequired' | 'CodeRequired';

export interface LoginResponse {
  multiFactorStatus: MultiFactorStatus;
  multiFactorToken: string;
  multiFactorTokenExpiration: string;
}

export interface SetupOtpAuthenticatorResponse {
  secretKey: string;
  qrCodeUri: string; // otpauth://totp/...
}

export interface TokenResponse {
  accessToken: string;
  expiration: string;
  refreshToken: string;
  refreshTokenExpiration: string;
}

export interface AssignOperationClaimRequest {
  userId: string;
  operationClaimName: string;
}

// ---------- Product ----------
export interface Product {
  id: string;
  name: string | null;
  amount: number;
  currency: string;
  stock: number;
}

export interface ProductUpsertRequest {
  name: string;
  amount: number;
  currency: string;
  stock: number;
}

// ---------- Order ----------
export interface Order {
  id: string;
  orderDate: string;
  totalPrice: number;
  currency: string;
}

export interface CreateOrderRequest {
  currency: string;
  items: { productId: string; amount: number }[];
}

export interface UpdateOrderRequest {
  orderDate: string;
}

// ---------- OrderItem ----------
export interface OrderItem {
  id: string;
  orderId: string;
  productId: string;
  amount: number;
  totalPrice: number;
  currency: string;
}

export interface CreateOrderItemRequest {
  orderId: string;
  productId: string;
  amount: number;
}

export interface UpdateOrderItemRequest {
  amount: number;
}

// ---------- Hata ----------
// BusinessExceptionMiddleware / LeakyBucketRateLimitingMiddleware / [ApiController] validasyonu.
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
