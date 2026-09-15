import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { api, refresh, setToken, logout } from "./api";
import type { Profile, Nav, Tokens } from "./types";
type Auth = {
  profile: Profile | null;
  permissions: string[];
  menus: Nav[];
  busy: boolean;
  error: unknown;
  accept: (tokens: Tokens) => Promise<void>;
  reload: () => Promise<void>;
  signOut: () => Promise<void>;
  end: () => void;
};
const Context = createContext<Auth>(null!);
export const useAuth = () => useContext(Context);
let startup: Promise<boolean> | undefined;
export function AuthProvider({ children }: { children: ReactNode }) {
  const cache = useQueryClient();
  const [profile, setProfile] = useState<Profile | null>(null);
  const [permissions, setPermissions] = useState<string[]>([]);
  const [menus, setMenus] = useState<Nav[]>([]);
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState<unknown>();
  const end = () => {
    setToken(null);
    setProfile(null);
    setPermissions([]);
    setMenus([]);
    cache.clear();
  };
  const reload = async () => {
    const p = await api<Profile>("/auth/me");
    const [grants, nav] = await Promise.all([
      api<string[]>("/me/permissions"),
      api<Nav[]>("/me/menus"),
    ]);
    setProfile(p);
    setPermissions(grants);
    setMenus(nav);
  };
  useEffect(() => {
    let active = true;
    startup ??= refresh();
    startup
      .then(async (ok) => {
        if (ok && active) await reload();
      })
      .catch((e) => {
        if (active) setError(e);
      })
      .finally(() => {
        if (active) setBusy(false);
      });
    const listener = () => end();
    window.addEventListener("session-ended", listener);
    return () => {
      active = false;
      window.removeEventListener("session-ended", listener);
    };
  }, []);
  return (
    <Context.Provider
      value={{
        profile,
        permissions,
        menus,
        busy,
        error,
        accept: async (tokens) => {
          if (!tokens.accessToken) throw new Error("Sign-in is incomplete.");
          setToken(tokens.accessToken);
          cache.clear();
          await reload();
        },
        reload,
        signOut: async () => {
          await logout();
          end();
        },
        end,
      }}
    >
      {children}
    </Context.Provider>
  );
}
