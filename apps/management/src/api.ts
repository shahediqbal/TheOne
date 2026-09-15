import type { Tokens } from "./types";
let accessToken: string | null = null;
let refreshPending: Promise<boolean> | null = null;
export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message);
  }
}
type Envelope<T> = {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
};
async function send<T>(
  path: string,
  method: string,
  body?: unknown,
  authorized = true,
): Promise<T> {
  const response = await fetch("/api/v1" + path, {
    method,
    credentials: "same-origin",
    headers: {
      "Content-Type": "application/json",
      "X-TheOne-Client": "web",
      ...(authorized && accessToken
        ? { Authorization: "Bearer " + accessToken }
        : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
    cache: "no-store",
  });
  let payload: Envelope<T>;
  try {
    payload = await response.json();
  } catch {
    throw new ApiError(
      "The server returned an unreadable response.",
      response.status,
    );
  }
  if (!response.ok || !payload.success)
    throw new ApiError(
      [payload.message, ...(payload.errors || [])].filter(Boolean).join(" "),
      response.status,
    );
  return payload.data;
}
export const setToken = (token: string | null) => {
  accessToken = token;
};
export function refresh(): Promise<boolean> {
  if (!refreshPending) {
    const exchange = async () => {
      try {
        const data = await send<Tokens>(
          "/browser/auth/refresh",
          "POST",
          {},
          false,
        );
        accessToken = data.accessToken;
        return !!accessToken;
      } catch (e) {
        if (e instanceof ApiError && e.status === 401) {
          accessToken = null;
          return false;
        }
        throw e;
      }
    };
    refreshPending = (
      navigator.locks
        ? navigator.locks.request("theone-refresh", exchange)
        : exchange()
    ).finally(() => {
      refreshPending = null;
    });
  }
  return refreshPending;
}
export async function api<T>(
  path: string,
  method = "GET",
  body?: unknown,
): Promise<T> {
  try {
    return await send<T>(path, method, body);
  } catch (e) {
    if (
      e instanceof ApiError &&
      e.status === 401 &&
      !path.startsWith("/browser/auth/")
    ) {
      if (await refresh()) return send<T>(path, method, body);
      window.dispatchEvent(new Event("session-ended"));
    }
    throw e;
  }
}
export const publicApi = <T>(path: string, body: unknown) =>
  send<T>(path, "POST", body, false);
export async function logout() {
  try {
    await publicApi("/browser/auth/logout", {});
  } finally {
    accessToken = null;
    window.dispatchEvent(new Event("session-ended"));
  }
}
