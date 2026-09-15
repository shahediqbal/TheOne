import { useState, type ReactNode } from "react";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  LinearProgress,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useTranslation } from "react-i18next";
export function Heading({
  title,
  subtitle,
  action,
}: {
  title: string;
  subtitle?: string;
  action?: ReactNode;
}) {
  const { t } = useTranslation();
  return (
    <Stack
      direction={{
        xs: "column",
        sm: "row",
      }}
      sx={{
        justifyContent: "space-between",
        gap: 2,
        mb: 3,
      }}
    >
      <Box>
        <Typography variant="h4">{t(title)}</Typography>
        {subtitle && (
          <Typography
            sx={{
              color: "text.secondary",
              ...{
                mt: 1,
              },
            }}
          >
            {t(subtitle)}
          </Typography>
        )}
      </Box>
      {action}
    </Stack>
  );
}
export function ErrorBox({ error }: { error: unknown }) {
  return error ? (
    <Alert severity="error" role="alert">
      {error instanceof Error ? error.message : String(error)}
    </Alert>
  ) : null;
}
export function Loading() {
  return <LinearProgress aria-label="Loading" />;
}
export function Empty() {
  const { t } = useTranslation();
  return (
    <Box
      sx={{
        color: "text.secondary",
        ...{
          py: 5,
          textAlign: "center",
        },
      }}
    >
      {t("No results")}
    </Box>
  );
}
export type Field = {
  name: string;
  label: string;
  type?: string;
  required?: boolean;
  minLength?: number;
  autoComplete?: string;
};
export function Form({
  fields,
  onSubmit,
  label = "Save",
  children,
}: {
  fields: Field[];
  onSubmit: (values: Record<string, string>) => Promise<void>;
  label?: string;
  children?: ReactNode;
}) {
  const { t } = useTranslation();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>();
  return (
    <Box
      component="form"
      onSubmit={async (e) => {
        e.preventDefault();
        const form = e.currentTarget;
        setBusy(true);
        setError(undefined);
        try {
          await onSubmit(
            Object.fromEntries(new FormData(form)) as Record<string, string>,
          );
        } catch (err) {
          setError(err);
        } finally {
          setBusy(false);
        }
      }}
    >
      <Stack spacing={2.5}>
        <ErrorBox error={error} />
        {fields.map((f) => (
          <TextField
            key={f.name}
            name={f.name}
            label={t(f.label)}
            type={f.type || "text"}
            required={f.required !== false}
            autoComplete={f.autoComplete || "off"}
            slotProps={{
              htmlInput: {
                minLength: f.minLength,
                maxLength: f.type === "password" ? 128 : 256,
              },
            }}
          />
        ))}
        {children}
        <Button type="submit" variant="contained" disabled={busy}>
          {busy ? t("Loading…") : t(label)}
        </Button>
      </Stack>
    </Box>
  );
}
export function Confirm({
  title,
  body,
  onConfirm,
  onClose,
}: {
  title: string;
  body: string;
  onConfirm: () => Promise<void>;
  onClose: () => void;
}) {
  const { t } = useTranslation();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>();
  return (
    <Dialog open onClose={() => !busy && onClose()} fullWidth maxWidth="xs">
      <DialogTitle>{t(title)}</DialogTitle>
      <DialogContent>
        <Typography
          sx={{
            mb: 2,
          }}
        >
          {body}
        </Typography>
        <ErrorBox error={error} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>
          {t("Cancel")}
        </Button>
        <Button
          color="error"
          disabled={busy}
          onClick={async () => {
            setBusy(true);
            try {
              await onConfirm();
              onClose();
            } catch (e) {
              setError(e);
              setBusy(false);
            }
          }}
        >
          {t("Confirm")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
export function Pager({
  page,
  total,
  onPage,
  size = 20,
}: {
  page: number;
  total: number;
  onPage: (n: number) => void;
  size?: number;
}) {
  const { t } = useTranslation();
  return (
    <Stack
      direction="row"
      sx={{
        alignItems: "center",
        justifyContent: "flex-end",
        gap: 2,
        mt: 2,
      }}
    >
      <Typography variant="body2">
        {page} / {Math.max(1, Math.ceil(total / size))} · {total}
      </Typography>
      <Button disabled={page === 1} onClick={() => onPage(page - 1)}>
        {t("Previous")}
      </Button>
      <Button disabled={page * size >= total} onClick={() => onPage(page + 1)}>
        {t("Next")}
      </Button>
    </Stack>
  );
}
