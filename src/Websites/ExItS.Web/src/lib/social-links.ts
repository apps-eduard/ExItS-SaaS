/**
 * Verified social profile URLs only.
 * Empty array = no social icons rendered in the footer.
 * Add entries when handles are confirmed — never invent destinations.
 */
export type SocialNetwork =
  | "facebook"
  | "instagram"
  | "linkedin"
  | "x"
  | "youtube"
  | "tiktok";

export type SocialLink = {
  network: SocialNetwork;
  href: string;
  label: string;
};

export const socialLinks: SocialLink[] = [];
