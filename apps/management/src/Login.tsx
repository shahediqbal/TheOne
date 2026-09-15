import Enrollment from "./Enrollment";
import { useState } from "react";
import { Box, Button, Chip, Divider, Stack, Typography } from "@mui/material";
import { useTranslation } from "react-i18next";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { TextField } from "@mui/material";
import { useAuth } from "./auth";
import { publicApi } from "./api";
import { Form, ErrorBox } from "./ui";
import type { Tokens } from "./types";
const schema = z.object({
  userNameOrMobile: z.string().min(1, "Enter your email or mobile."),
  password: z.string().min(1, "Enter your password."),
});
export default function Login() {
  const { t, i18n } = useTranslation();
  const auth = useAuth();
  const [mode, setMode] = useState("login");
  const [challenge, setChallenge] = useState<string | null>(null);
  const [recovery, setRecovery] = useState(false);
  const [error, setError] = useState<unknown>();
  const [notice, setNotice] = useState("");
  const form = useForm<z.infer<typeof schema>>({
    resolver: zodResolver(schema),
  });
  const change = (m: string) => {
    setMode(m);
    setChallenge(null);
    setError(undefined);
    setNotice("");
    form.reset();
  };
  const complete = async (result: Tokens) => {
    if (result.requiresAuthenticatorSetup) {
      setChallenge(result.challengeId);
      setMode("enroll");
      form.reset();
    } else if (result.requiresTwoFactor) {
      setChallenge(result.challengeId);
      setMode("mfa");
      form.reset();
    } else if (result.accessToken) await auth.accept(result);
    else throw new Error("Sign-in could not be completed.");
  };
  return (
    <Box
      sx={{
        gridTemplateColumns: {
          xs: "1fr",
          md: "44% 56%",
        },
        ...{
          minHeight: "100vh",
          display: "grid",
        },
      }}
    >
      <Box
        className="auth-art"
        sx={{
          display: {
            xs: "none",
            md: "flex",
          },
          p: {
            md: 6,
            lg: 9,
          },
          flexDirection: "column",
          justifyContent: "space-between",
        }}
      >
        <Stack
          direction="row"
          sx={{
            gap: 2,
            alignItems: "center",
          }}
        >
          <span className="brand-mark">১</span>
          <Typography variant="h5">The One</Typography>
        </Stack>
        <Box
          sx={{
            zIndex: 1,
          }}
        >
          <Chip
            label="MANAGEMENT PORTAL"
            sx={{
              color: "#e8c47b",
              border: "1px solid #e8c47b55",
              mb: 3,
            }}
          />
          <Typography
            component="h1"
            sx={{
              fontSize: {
                md: 44,
                lg: 56,
              },
              lineHeight: 1.12,
              fontWeight: 650,
            }}
          >
            One place.
            <br />
            Shared purpose.
          </Typography>
          <Typography
            sx={{
              color: "#c0d4d8",
              ...{
                mt: 3,
                maxWidth: 320,
                lineHeight: 1.8,
              },
            }}
          >
            Manage your account and the work you have access to.
          </Typography>
        </Box>
        <Typography
          variant="body2"
          sx={{
            color: "#b1c7cc",
          }}
        >
          The One · Secure workspace
        </Typography>
      </Box>
      <Box
        sx={{
          display: "flex",
          flexDirection: "column",
          minHeight: "100vh",
          p: {
            xs: 3,
            sm: 5,
          },
        }}
      >
        <Box
          sx={{
            textAlign: "right",
          }}
        >
          <Button
            onClick={() =>
              i18n.changeLanguage(i18n.language === "en" ? "bn" : "en")
            }
          >
            {i18n.language === "en" ? "বাংলা" : "English"}
          </Button>
        </Box>
        <Box
          sx={{
            width: "100%",
            maxWidth: 420,
            m: "auto",
            py: 5,
          }}
        >
          <Typography
            variant="overline"
            sx={{
              color: "primary",
            }}
          >
            THE ONE
          </Typography>
          <Typography
            variant="h4"
            sx={{
              mt: 1,
              mb: 1,
            }}
          >
            {t(
              mode === "login"
                ? "Sign in"
                : mode === "mfa"
                  ? "Verify your identity"
                  : mode === "register"
                    ? "Create account"
                    : mode === "sms"
                      ? "Use SMS code"
                      : mode === "forgot"
                        ? "Reset password"
                        : "Enter your code",
            )}
          </Typography>
          <Typography
            sx={{
              color: "text.secondary",
              ...{
                mb: 4,
              },
            }}
          >
            {mode === "login"
              ? t("Use your account to continue.")
              : mode === "mfa"
                ? t(
                    "Enter a current authenticator code or use a saved recovery code.",
                  )
                : t("Follow the steps to continue.")}
          </Typography>
          <ErrorBox error={error || auth.error} />
          {notice && (
            <Box
              role="status"
              sx={{
                mb: 3,
              }}
            >
              {notice}
            </Box>
          )}
          {mode === "login" && (
            <Box
              component="form"
              onSubmit={form.handleSubmit(async (values) => {
                setError(undefined);
                try {
                  await complete(
                    await publicApi<Tokens>("/browser/auth/login", values),
                  );
                } catch (e) {
                  setError(e);
                }
              })}
            >
              <Stack spacing={2.5}>
                <TextField
                  label={t("Email or mobile")}
                  autoComplete="username"
                  {...form.register("userNameOrMobile")}
                  error={!!form.formState.errors.userNameOrMobile}
                  helperText={form.formState.errors.userNameOrMobile?.message}
                />
                <TextField
                  label={t("Password")}
                  type="password"
                  autoComplete="current-password"
                  {...form.register("password")}
                  error={!!form.formState.errors.password}
                  helperText={form.formState.errors.password?.message}
                />
                <Button
                  type="submit"
                  variant="contained"
                  disabled={form.formState.isSubmitting}
                >
                  {t("Sign in")}
                </Button>
                <Button onClick={() => change("forgot")}>
                  {t("Forgot password?")}
                </Button>
                <Divider />
                <Button variant="outlined" onClick={() => change("sms")}>
                  {t("Use SMS code")}
                </Button>
                <Button onClick={() => change("register")}>
                  {t("Create account")}
                </Button>
              </Stack>
            </Box>
          )}
          {mode === "enroll" && challenge && (
            <Enrollment
              challengeId={challenge}
              onDone={() => change("login")}
            />
          )}
          {mode === "mfa" && (
            <>
              <Form
                key={String(recovery)}
                fields={[
                  {
                    name: "code",
                    label: recovery ? "Recovery code" : "Authenticator code",
                    autoComplete: "one-time-code",
                  },
                ]}
                label="Continue"
                onSubmit={async (v) =>
                  complete(
                    await publicApi<Tokens>(
                      recovery
                        ? "/browser/auth/recovery-code"
                        : "/browser/auth/authenticator",
                      {
                        challengeId: challenge,
                        ...(recovery
                          ? {
                              recoveryCode: v.code,
                            }
                          : {
                              code: v.code,
                            }),
                      },
                    ),
                  )
                }
              />
              <Button
                sx={{
                  mt: 2,
                }}
                onClick={() => setRecovery(!recovery)}
              >
                {t(recovery ? "Use authenticator code" : "Use a recovery code")}
              </Button>
            </>
          )}
          {mode === "register" && (
            <Form
              label="Create account"
              fields={[
                {
                  name: "fullName",
                  label: "Full name",
                },
                {
                  name: "email",
                  label: "Email",
                  type: "email",
                },
                {
                  name: "mobileNumber",
                  label: "Mobile number",
                  type: "tel",
                },
                {
                  name: "password",
                  label: "Password",
                  type: "password",
                  minLength: 8,
                },
                {
                  name: "confirmPassword",
                  label: "Confirm password",
                  type: "password",
                },
              ]}
              onSubmit={async (v) => {
                if (v.password !== v.confirmPassword)
                  throw new Error("Passwords do not match.");
                await publicApi("/auth/register", v);
                change("login");
                setNotice("Account created. Sign in to continue.");
              }}
            />
          )}
          {(mode === "sms" || mode === "forgot") && (
            <Form
              label="Continue"
              fields={[
                {
                  name: mode === "sms" ? "mobileNumber" : "userNameOrMobile",
                  label: mode === "sms" ? "Mobile number" : "Email or mobile",
                },
              ]}
              onSubmit={async (v) => {
                const data = await publicApi<{
                  challengeId: string;
                }>(
                  mode === "sms"
                    ? "/auth/request-login-otp"
                    : "/auth/forgot-password",
                  v,
                );
                setChallenge(data.challengeId);
                setMode(mode === "sms" ? "sms-code" : "reset");
                setNotice(
                  "If your account is eligible, a code will be sent to its verified mobile.",
                );
              }}
            />
          )}
          {mode === "sms-code" && (
            <Form
              label="Sign in"
              fields={[
                {
                  name: "code",
                  label: "SMS code",
                  autoComplete: "one-time-code",
                },
              ]}
              onSubmit={async (v) =>
                complete(
                  await publicApi<Tokens>("/browser/auth/otp", {
                    challengeId: challenge,
                    code: v.code,
                  }),
                )
              }
            />
          )}
          {mode === "reset" && (
            <Form
              label="Reset password"
              fields={[
                {
                  name: "code",
                  label: "SMS code",
                  autoComplete: "one-time-code",
                },
                {
                  name: "newPassword",
                  label: "New password",
                  type: "password",
                  minLength: 8,
                },
                {
                  name: "confirmPassword",
                  label: "Confirm password",
                  type: "password",
                },
              ]}
              onSubmit={async (v) => {
                if (v.newPassword !== v.confirmPassword)
                  throw new Error("Passwords do not match.");
                await publicApi("/auth/reset-password", {
                  ...v,
                  challengeId: challenge,
                });
                change("login");
                setNotice("Password reset. Sign in again.");
              }}
            />
          )}
          {mode !== "login" && (
            <Button
              fullWidth
              sx={{
                mt: 3,
              }}
              onClick={() => change("login")}
            >
              {t("Back to sign in")}
            </Button>
          )}
        </Box>
        <Typography
          variant="body2"
          sx={{
            color: "text.secondary",
            ...{
              textAlign: "center",
            },
          }}
        >
          The One
        </Typography>
      </Box>
    </Box>
  );
}
