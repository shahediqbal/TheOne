export type Tokens = {
  accessToken: string | null;
  expiresAtUtc: string;
  requiresTwoFactor: boolean;
  requiresAuthenticatorSetup: boolean;
  challengeId: string | null;
};
export type Profile = {
  userId: string;
  fullName: string;
  email: string;
  mobileNumber: string;
  emailConfirmed: boolean;
  mobileConfirmed: boolean;
  authenticatorEnabled: boolean;
  roles: string[];
};
export type User = {
  id: string;
  fullName: string;
  email: string;
  mobileNumber: string;
  isActive: boolean;
  emailVerified: boolean;
  mobileVerified: boolean;
  authenticatorEnabled: boolean;
  createdAtUtc: string;
  roles: string[];
};
export type Page<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};
export type Role = {
  id: string;
  name: string;
  isSystem: boolean;
  permissions: string[];
};
export type Menu = {
  id: string;
  labelEn: string;
  labelBn: string | null;
  icon: string | null;
  route: string | null;
  parentId: string | null;
  sortOrder: number;
  enabled: boolean;
  requiredPermission: string | null;
  roleIds: string[];
};
export type Nav = {
  id: string;
  labelEn: string;
  labelBn: string | null;
  route: string | null;
  children: Nav[];
};
export type Audit = {
  id: string;
  createdAtUtc: string;
  actorId: string | null;
  action: string;
  target: string;
  details: string;
};
export type Session = {
  sessionId: string;
  createdAtUtc: string;
  expiresAtUtc: string;
  lastRefreshedAtUtc: string;
  isCurrent: boolean;
};
