import { Navigate, useLocation, useSearchParams } from "react-router-dom";
import type { UserDirectoryFilter } from "@/api/users/user-types";

export function UsersDirectoryRedirect({ directory }: { directory: UserDirectoryFilter }) {
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const next = new URLSearchParams(searchParams);
  next.set("directory", directory);
  const search = next.toString();
  return (
    <Navigate
      to={{ pathname: "/admin/users", search: search ? `?${search}` : "" }}
      replace
      state={location.state}
    />
  );
}
