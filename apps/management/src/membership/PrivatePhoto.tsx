import { useEffect, useState } from "react";
import { Button, Box } from "@mui/material";
import { useTranslation } from "react-i18next";
import { apiBlob } from "../api";
import { ErrorBox } from "../ui";
export default function PrivatePhoto({ reference }: { reference: string }) {
  const { i18n } = useTranslation();
  const [url, setUrl] = useState(""),
    [error, setError] = useState<unknown>(),
    [busy, setBusy] = useState(false);
  useEffect(
    () => () => {
      if (url) URL.revokeObjectURL(url);
    },
    [url],
  );
  return (
    <Box>
      <ErrorBox error={error} />
      {url ? (
        <img
          src={url}
          alt={
            i18n.language.startsWith("bn")
              ? "আবেদনকারীর ছবি"
              : "Applicant photo"
          }
          style={{ width: 150, height: 190, objectFit: "cover" }}
        />
      ) : (
        <Button
          disabled={busy}
          onClick={async () => {
            setBusy(true);
            try {
              setUrl(
                URL.createObjectURL(
                  await apiBlob(
                    "/admin/membership/applications/" + reference + "/photo",
                  ),
                ),
              );
            } catch (e) {
              setError(e);
            } finally {
              setBusy(false);
            }
          }}
        >
          {i18n.language.startsWith("bn") ? "ছবি দেখুন" : "View photo"}
        </Button>
      )}
    </Box>
  );
}
