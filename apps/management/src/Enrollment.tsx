import { useEffect, useState } from "react";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import { publicApi } from "./api";
import { ErrorBox, Form, Loading } from "./ui";
export default function Enrollment({
  challengeId,
  onDone,
}: {
  challengeId: string;
  onDone: () => void;
}) {
  const [setup, setSetup] = useState<{
    sharedKey: string;
    qrCodeDataUri: string;
  }>();
  const [codes, setCodes] = useState<string[]>();
  const [error, setError] = useState<unknown>();
  useEffect(() => {
    publicApi<{
      sharedKey: string;
      qrCodeDataUri: string;
    }>("/auth/authenticator/setup", {
      challengeId,
    })
      .then(setSetup)
      .catch(setError);
  }, [challengeId]);
  if (codes)
    return (
      <Stack spacing={2}>
        <Alert severity="success">
          Authenticator enabled. Save these recovery codes privately before
          continuing.
        </Alert>
        <Box className="codes">
          {codes.map((c) => (
            <code key={c}>{c}</code>
          ))}
        </Box>
        <Button variant="contained" onClick={onDone}>
          I saved my codes — sign in again
        </Button>
      </Stack>
    );
  return (
    <Stack spacing={2}>
      <ErrorBox error={error} />
      {!setup && !error && <Loading />}
      {setup && (
        <>
          <Typography>
            Scan this QR code with your authenticator app, or enter the setup
            key manually.
          </Typography>
          <Box
            component="img"
            src={setup.qrCodeDataUri}
            alt="Authenticator setup QR code"
            sx={{
              width: 220,
              maxWidth: "100%",
              alignSelf: "center",
            }}
          />
          <Typography
            component="code"
            sx={{
              overflowWrap: "anywhere",
            }}
          >
            {setup.sharedKey}
          </Typography>
          <Form
            fields={[
              {
                name: "code",
                label: "Authenticator code",
                autoComplete: "one-time-code",
              },
            ]}
            label="Enable authenticator"
            onSubmit={async (v) => {
              const result = await publicApi<{
                recoveryCodes: string[];
              }>("/auth/authenticator/confirm-setup", {
                challengeId,
                code: v.code,
              });
              setSetup(undefined);
              setCodes(result.recoveryCodes);
            }}
          />
        </>
      )}
    </Stack>
  );
}
