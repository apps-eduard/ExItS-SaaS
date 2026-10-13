import { createContext, useContext, type ReactNode } from "react";

type PersonalAvatarValue = {
  photoUrl: string | null;
  setPhotoUrl: (url: string | null) => void;
};

const PersonalAvatarContext = createContext<PersonalAvatarValue | null>(null);

export function PersonalAvatarContextProvider({
  value,
  children,
}: {
  value: PersonalAvatarValue;
  children: ReactNode;
}) {
  return <PersonalAvatarContext.Provider value={value}>{children}</PersonalAvatarContext.Provider>;
}

export function usePersonalAvatarPhoto(): string | null {
  return useContext(PersonalAvatarContext)?.photoUrl ?? null;
}

export function useSetPersonalAvatarPhoto(): (url: string | null) => void {
  return useContext(PersonalAvatarContext)?.setPhotoUrl ?? (() => undefined);
}
