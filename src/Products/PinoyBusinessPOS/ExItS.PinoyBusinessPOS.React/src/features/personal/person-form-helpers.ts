import { PlatformApiError } from "@/api/platform/platform-http";
import type { PersonalContactDto, ResolvedPublicUserDto } from "@/api/platform/personal-types";

export function isPublicUserNotFound(error: unknown): boolean {
  if (!(error instanceof PlatformApiError)) {
    return false;
  }
  if (error.status === 404) {
    return true;
  }
  const code = error.errorCode ?? "";
  return (
    code === "application.user.not_found" ||
    code === "platform.public_user_id.invalid"
  );
}

export function isAlreadyAddedConflict(error: unknown): boolean {
  if (!(error instanceof PlatformApiError)) {
    return false;
  }
  const code = error.errorCode ?? "";
  return (
    code === "application.personal.contact.identity.conflict" ||
    (code === "application.personal.contact.email.conflict" &&
      /exits identity|already (exists|in your people)/i.test(error.message))
  );
}

export function findExistingContact(
  contacts: PersonalContactDto[] | undefined,
  resolved: ResolvedPublicUserDto,
): PersonalContactDto | null {
  if (!contacts?.length) {
    return null;
  }
  const publicId = resolved.publicUserId.trim().toUpperCase();
  return (
    contacts.find((contact) => {
      if (contact.resolvedUserIdentityId === resolved.userIdentityId) {
        return true;
      }
      if (contact.linkedUserIdentityId === resolved.userIdentityId) {
        return true;
      }
      const contactPublic = contact.resolvedPublicUserId?.trim().toUpperCase() ?? "";
      return contactPublic.length > 0 && contactPublic === publicId;
    }) ?? null
  );
}
