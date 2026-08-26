export type PlatformGeneralSettings = {
  platformDisplayName: string;
  supportEmail: string | null;
  brandingLogoUrl: string | null;
  brandingPrimaryColor: string | null;
  brandingAccentColor: string | null;
  version: number;
  updatedAtUtc: string;
  updatedByActorId: string | null;
};

export type UpdatePlatformGeneralSettingsBody = {
  platformDisplayName: string;
  supportEmail?: string | null;
  brandingLogoUrl?: string | null;
  brandingPrimaryColor?: string | null;
  brandingAccentColor?: string | null;
  expectedVersion?: number | null;
};

export type PlatformEmailSettings = {
  providerMode: string;
  smtpHost: string | null;
  smtpPort: number | null;
  smtpUsername: string | null;
  passwordConfigured: boolean;
  fromDisplayName: string;
  fromAddress: string;
  securityMode: string;
  adminPublicBaseUrl: string | null;
  isConfigured: boolean;
  version: number;
  updatedAtUtc: string;
  updatedByActorId: string | null;
};

export type UpdatePlatformEmailSettingsBody = {
  providerMode: string;
  smtpHost?: string | null;
  smtpPort?: number | null;
  smtpUsername?: string | null;
  replacePassword: boolean;
  smtpPassword?: string | null;
  fromDisplayName: string;
  fromAddress: string;
  securityMode: string;
  adminPublicBaseUrl?: string | null;
  expectedVersion?: number | null;
};

export type PlatformEmailTestBody = {
  recipientEmail: string;
};

export type PlatformEmailTestResult = {
  succeeded: boolean;
  message: string;
};

export type PlatformRegionalSettings = {
  defaultTimeZoneId: string;
  defaultLocale: string;
  defaultCurrencyCode: string;
  defaultCountryCode: string;
  dateFormat: string | null;
  timeFormat: string | null;
  version: number;
  updatedAtUtc: string;
  updatedByActorId: string | null;
};

export type UpdatePlatformRegionalSettingsBody = {
  defaultTimeZoneId: string;
  defaultLocale: string;
  defaultCurrencyCode: string;
  defaultCountryCode: string;
  dateFormat?: string | null;
  timeFormat?: string | null;
  expectedVersion?: number | null;
};
