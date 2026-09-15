import { useState } from "react";
import {
  Alert,
  Box,
  Button,
  Chip,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "../auth";
import { api } from "../api";
import { Confirm, ErrorBox, Form, Heading, Loading, Pager } from "../ui";
import Enrollment from "../Enrollment";
import type { Session } from "../types";
export default function Security() {
  const auth = useAuth();
  const cache = useQueryClient();
  const [page, setPage] = useState(1);
  const [mobile, setMobile] = useState<string>();
  const [enrollment, setEnrollment] = useState<string>();
  const [codes, setCodes] = useState<string[]>();
  const [notice, setNotice] = useState("");
  const [error, setError] = useState<unknown>();
  const [confirm, setConfirm] = useState<{
    title: string;
    run: () => Promise<void>;
  }>();
  const sessions = useQuery({
    queryKey: ["sessions", page],
    queryFn: () =>
      api<{
        items: Session[];
        totalCount: number;
      }>(`/auth/sessions?pageNumber=${page}&pageSize=20`),
    enabled: !enrollment && !codes,
  });
  const finish = () => {
    auth.signOut().catch(() => auth.end());
  };
  if (enrollment)
    return (
      <Box
        sx={{
          maxWidth: 520,
        }}
      >
        <Heading title="Set up authenticator" />
        <Enrollment challengeId={enrollment} onDone={finish} />
      </Box>
    );
  if (codes)
    return (
      <Box
        sx={{
          maxWidth: 520,
        }}
      >
        <Heading title="Recovery codes" />
        <Alert severity="success">
          Save your new recovery codes privately. Your previous codes no longer
          work.
        </Alert>
        <Box
          className="codes"
          sx={{
            my: 3,
          }}
        >
          {codes.map((c) => (
            <code key={c}>{c}</code>
          ))}
        </Box>
        <Button variant="contained" onClick={finish}>
          I saved my codes — sign in again
        </Button>
      </Box>
    );
  return (
    <>
      <Heading
        title="Account security"
        subtitle="Manage verification, your password and active sessions."
      />
      <Stack spacing={3}>
        <ErrorBox error={error} />
        {notice && <Alert severity="success">{notice}</Alert>}
        <Paper
          sx={{
            p: 3,
          }}
        >
          <Typography
            variant="h6"
            sx={{
              mb: 2,
            }}
          >
            Mobile verification
          </Typography>
          {auth.profile?.mobileConfirmed ? (
            <Chip label="Verified" color="success" />
          ) : mobile ? (
            <Form
              fields={[
                {
                  name: "code",
                  label: "SMS code",
                  autoComplete: "one-time-code",
                },
              ]}
              label="Verify mobile"
              onSubmit={async (v) => {
                await api("/auth/verify-mobile", "POST", {
                  challengeId: mobile,
                  code: v.code,
                });
                await auth.reload();
                setMobile(undefined);
                setNotice("Mobile verified.");
              }}
            />
          ) : (
            <Button
              variant="outlined"
              onClick={async () => {
                setError(undefined);
                try {
                  const result = await api<{
                    challengeId: string;
                  }>("/auth/request-mobile-verification", "POST", {});
                  setMobile(result.challengeId);
                } catch (e) {
                  setError(e);
                }
              }}
            >
              Send verification code
            </Button>
          )}
        </Paper>
        <Paper
          sx={{
            p: 3,
          }}
        >
          <Typography
            variant="h6"
            sx={{
              mb: 2,
            }}
          >
            Change password
          </Typography>
          <Box
            sx={{
              maxWidth: 480,
            }}
          >
            <Form
              fields={[
                {
                  name: "currentPassword",
                  label: "Current password",
                  type: "password",
                  autoComplete: "current-password",
                },
                {
                  name: "newPassword",
                  label: "New password",
                  type: "password",
                  minLength: 8,
                  autoComplete: "new-password",
                },
                {
                  name: "confirmPassword",
                  label: "Confirm password",
                  type: "password",
                  autoComplete: "new-password",
                },
              ]}
              label="Change password and sign out"
              onSubmit={async (v) => {
                if (v.newPassword !== v.confirmPassword)
                  throw new Error("Passwords do not match.");
                await api("/auth/change-password", "POST", v);
                finish();
              }}
            />
          </Box>
        </Paper>
        <Paper
          sx={{
            p: 3,
          }}
        >
          <Typography
            variant="h6"
            sx={{
              mb: 1,
            }}
          >
            Authenticator
          </Typography>
          <Typography
            sx={{
              color: "text.secondary",
              ...{
                mb: 3,
              },
            }}
          >
            Required for administrative access. Security changes sign you out of
            existing sessions.
          </Typography>
          <Box
            sx={{
              maxWidth: 480,
            }}
          >
            {!auth.profile?.authenticatorEnabled ? (
              <Form
                fields={[
                  {
                    name: "password",
                    label: "Password",
                    type: "password",
                  },
                ]}
                label="Set up authenticator"
                onSubmit={async (v) => {
                  const result = await api<{
                    challengeId: string;
                  }>("/auth/authenticator/begin-enrollment", "POST", v);
                  setEnrollment(result.challengeId);
                }}
              />
            ) : (
              <>
                <Chip
                  label="Enabled"
                  color="success"
                  sx={{
                    mb: 3,
                  }}
                />
                <Form
                  fields={[
                    {
                      name: "password",
                      label: "Password",
                      type: "password",
                    },
                    {
                      name: "code",
                      label: "Authenticator code",
                      required: false,
                    },
                    {
                      name: "recoveryCode",
                      label: "Recovery code",
                      required: false,
                    },
                  ]}
                  label="Replace authenticator"
                  onSubmit={async (v) => {
                    const result = await api<{
                      challengeId: string;
                    }>("/auth/authenticator/reset", "POST", factor(v));
                    setEnrollment(result.challengeId);
                  }}
                />
                <Divider
                  sx={{
                    my: 3,
                  }}
                />
                <Form
                  fields={[
                    {
                      name: "password",
                      label: "Password",
                      type: "password",
                    },
                    {
                      name: "code",
                      label: "Authenticator code",
                      required: false,
                    },
                    {
                      name: "recoveryCode",
                      label: "Recovery code",
                      required: false,
                    },
                  ]}
                  label="Generate new recovery codes"
                  onSubmit={async (v) => {
                    const result = await api<{
                      recoveryCodes: string[];
                    }>(
                      "/auth/authenticator/regenerate-recovery-codes",
                      "POST",
                      factor(v),
                    );
                    setCodes(result.recoveryCodes);
                  }}
                />
              </>
            )}
          </Box>
        </Paper>
        <Paper
          sx={{
            p: 3,
          }}
        >
          <Heading
            title="Active sessions"
            action={
              <Button
                color="error"
                onClick={() =>
                  setConfirm({
                    title: "Sign out all sessions?",
                    run: async () => {
                      await api("/auth/logout-all", "POST", {});
                      finish();
                    },
                  })
                }
              >
                Sign out all
              </Button>
            }
          />
          <ErrorBox error={sessions.error} />
          {sessions.isLoading && <Loading />}
          {sessions.data?.items.map((s) => (
            <Stack
              key={s.sessionId}
              direction={{
                xs: "column",
                sm: "row",
              }}
              sx={{
                gap: 2,
                justifyContent: "space-between",
                py: 2,
                borderBottom: "1px solid #e3e9ed",
              }}
            >
              <Box>
                <Typography>
                  {s.isCurrent ? "This session" : "Other session"}
                </Typography>
                <Typography
                  variant="body2"
                  sx={{
                    color: "text.secondary",
                  }}
                >
                  Started {new Date(s.createdAtUtc).toLocaleString()}
                </Typography>
              </Box>
              <Button
                color="error"
                onClick={() =>
                  setConfirm({
                    title: "Revoke session?",
                    run: async () => {
                      await api("/auth/sessions/" + s.sessionId, "DELETE");
                      if (s.isCurrent) finish();
                      else
                        await cache.invalidateQueries({
                          queryKey: ["sessions"],
                        });
                    },
                  })
                }
              >
                Revoke
              </Button>
            </Stack>
          ))}
          <Pager
            page={page}
            total={sessions.data?.totalCount || 0}
            onPage={setPage}
          />
        </Paper>
      </Stack>
      {confirm && (
        <Confirm
          title={confirm.title}
          body="This will end access for the selected sessions."
          onConfirm={confirm.run}
          onClose={() => setConfirm(undefined)}
        />
      )}
    </>
  );
}
function factor(v: Record<string, string>) {
  if (!!v.code === !!v.recoveryCode)
    throw new Error("Enter either an authenticator code or a recovery code.");
  return {
    password: v.password,
    ...(v.code
      ? {
          code: v.code,
        }
      : {
          recoveryCode: v.recoveryCode,
        }),
  };
}
