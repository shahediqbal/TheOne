import { useRef, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Stack,
  TextField,
  MenuItem,
  Typography,
} from "@mui/material";
import { useTranslation } from "react-i18next";
import { api } from "../api";
import { ErrorBox } from "../ui";
import type { Detail } from "./model";
import {
  fields,
  helpOptions,
  validate,
  conduct,
  declaration,
  oath,
  consentVersion,
  type Values,
} from "../../../../packages/membership-form/schema";
export default function OperatorEntry({
  onClose,
  onSaved,
}: {
  onClose: () => void;
  onSaved: (result: Detail) => Promise<void>;
}) {
  const { i18n, t } = useTranslation();
  const lang = i18n.language.startsWith("bn") ? "bn" : "en";
  const [values, setValues] = useState<Values>({}),
    [consents, setConsents] = useState([false, false, false]),
    [review, setReview] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<unknown>(),
    [photo, setPhoto] = useState(""),
    [photoName, setPhotoName] = useState("");
  const [requestId] = useState(() => crypto.randomUUID());
  const saving = useRef(false);
  const change = (key: string, value: string | number) =>
    setValues((v) => ({ ...v, [key]: value }));
  async function save() {
    if (saving.current) return;
    saving.current = true;
    setBusy(true);
    setError(undefined);
    try {
      const result = await api<Detail>("/admin/membership/operator", "POST", {
        requestId,
        fields: Object.fromEntries(
          Object.entries(values).filter(
            ([, v]) => v !== "" && v !== null && v !== undefined,
          ),
        ),
        codeOfConductAccepted: consents[0],
        declarationAccepted: consents[1],
        oathAccepted: consents[2],
        consentVersion,
        photoBase64: photo,
      });
      await onSaved(result);
    } catch (e) {
      setError(e);
    } finally {
      saving.current = false;
      setBusy(false);
    }
  }
  return (
    <Dialog open onClose={() => !busy && onClose()} fullWidth maxWidth="md">
      <DialogTitle>
        {t(review ? "Review paper application" : "Enter paper application")}
      </DialogTitle>
      <DialogContent>
        <ErrorBox error={error} />
        {review ? (
          <Stack spacing={2}>
            <Alert severity="info">
              {t(
                "This creates a submitted application. Verification and approval are separate steps.",
              )}
            </Alert>
            {fields
              .filter(
                (f) =>
                  values[f.key] !== undefined &&
                  values[f.key] !== "" &&
                  (!f.otherOnly || !!(Number(values.helpCategories) & 64)),
              )
              .map((f) => (
                <Box key={f.key}>
                  <Typography color="text.secondary">{f[lang]}</Typography>
                  <Typography
                    sx={{ whiteSpace: "pre-wrap", overflowWrap: "anywhere" }}
                  >
                    {f.options?.find(
                      (o) => String(o[0]) === String(values[f.key]),
                    )?.[lang === "bn" ? 1 : 2] ?? values[f.key]}
                  </Typography>
                </Box>
              ))}
            <Typography>
              {t("Areas of help")}:{" "}
              {helpOptions
                .filter(([bit]) => !!(Number(values.helpCategories) & bit))
                .map((o) => o[lang === "bn" ? 1 : 2])
                .join(", ")}
            </Typography>
            <Typography>
              {lang === "bn" ? "ছবি" : "Photo"}: {photoName}
            </Typography>
            <Alert severity="success">
              {lang === "bn"
                ? "আচরণবিধি, ঘোষণা ও শপথ—তিনটি সম্মতিই নথিভুক্ত।"
                : "Conduct, declaration and oath acceptance recorded from the paper form."}
            </Alert>
          </Stack>
        ) : (
          <Box
            component="form"
            id="paper-entry"
            noValidate
            onSubmit={(e) => {
              e.preventDefault();
              const errors = validate(values);
              if (errors.length || !photo || !consents.every(Boolean)) {
                setError(
                  new Error(
                    (lang === "bn"
                      ? "পূরণ বা সংশোধন করুন: "
                      : "Complete or correct: ") +
                      [
                        ...errors.map(
                          (k) => fields.find((f) => f.key === k)?.[lang] || k,
                        ),
                        ...(!photo ? ["Photo"] : []),
                        ...(!consents.every(Boolean)
                          ? ["All three consent statements"]
                          : []),
                      ].join(", "),
                  ),
                );
                return;
              }
              setError(undefined);
              setReview(true);
            }}
          >
            <Typography sx={{ mb: 2 }}>
              {lang === "bn"
                ? "আবেদনকারীর কাগজের ফর্ম থেকে তথ্য ও সম্মতি নথিভুক্ত করুন।"
                : "Transcribe the applicant’s details and consent from the paper form."}
            </Typography>
            <Box
              sx={{
                display: "grid",
                gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" },
                gap: 2,
              }}
            >
              {fields
                .filter(
                  (f) => !f.otherOnly || !!(Number(values.helpCategories) & 64),
                )
                .map((f) => (
                  <TextField
                    key={f.key}
                    name={f.key}
                    label={f[lang]}
                    required={f.required}
                    value={values[f.key] ?? ""}
                    type={
                      f.type === "select" || f.type === "textarea"
                        ? "text"
                        : f.type || "text"
                    }
                    select={f.type === "select"}
                    multiline={f.type === "textarea"}
                    minRows={f.type === "textarea" ? 2 : undefined}
                    onChange={(e) =>
                      change(
                        f.key,
                        (f.type === "number" ||
                          typeof f.options?.[0]?.[0] === "number") &&
                          e.target.value !== ""
                          ? Number(e.target.value)
                          : e.target.value,
                      )
                    }
                    slotProps={{
                      inputLabel:
                        f.type === "date" ? { shrink: true } : undefined,
                      htmlInput: {
                        min: f.min,
                        max: f.type === "number" ? f.max : undefined,
                        maxLength:
                          f.type === "number" ? undefined : (f.max ?? 200),
                      },
                    }}
                  >
                    {f.type === "select"
                      ? [
                          <MenuItem key="empty" value="">
                            {t("Select")}
                          </MenuItem>,
                          ...f.options!.map((o) => (
                            <MenuItem key={o[0]} value={o[0]}>
                              {o[lang === "bn" ? 1 : 2]}
                            </MenuItem>
                          )),
                        ]
                      : undefined}
                  </TextField>
                ))}
            </Box>
            <Typography sx={{ mt: 3 }}>{t("Areas of help")} *</Typography>
            {helpOptions.map(([bit, bn, en]) => (
              <FormControlLabel
                key={bit}
                label={lang === "bn" ? bn : en}
                control={
                  <Checkbox
                    checked={!!(Number(values.helpCategories) & bit)}
                    onChange={(e) =>
                      change(
                        "helpCategories",
                        e.target.checked
                          ? Number(values.helpCategories) | bit
                          : Number(values.helpCategories) & ~bit,
                      )
                    }
                  />
                }
              />
            ))}
            <Box sx={{ my: 3 }}>
              <label>
                {lang === "bn"
                  ? "আবেদনকারীর ছবি (JPEG/PNG, ১০ MB) *"
                  : "Applicant photo (JPEG/PNG, 10 MB) *"}
                <input
                  type="file"
                  accept="image/jpeg,image/png"
                  onChange={(e) => {
                    const file = e.target.files?.[0];
                    setPhoto("");
                    setPhotoName("");
                    if (!file) return;
                    if (
                      file.size > 10 * 1024 * 1024 ||
                      !["image/jpeg", "image/png"].includes(file.type)
                    ) {
                      setError(
                        new Error("Choose a JPEG or PNG image up to 10 MB."),
                      );
                      return;
                    }
                    const reader = new FileReader();
                    reader.onload = () => {
                      setPhoto(String(reader.result).split(",")[1]);
                      setPhotoName(file.name);
                    };
                    reader.onerror = () =>
                      setError(new Error("Photo could not be read."));
                    reader.readAsDataURL(file);
                  }}
                />
              </label>
            </Box>
            <Typography>
              {lang === "bn"
                ? "কাগজের ফর্মে আবেদনকারী প্রতিটি বিবৃতিতে সম্মতি দিয়েছেন:"
                : "The paper form records the applicant’s acceptance of each statement:"}
            </Typography>
            {[conduct, declaration, oath].map((statement, i) => (
              <FormControlLabel
                key={i}
                sx={{
                  display: "flex",
                  alignItems: "start",
                  whiteSpace: "pre-line",
                  my: 2,
                }}
                label={statement[lang]}
                control={
                  <Checkbox
                    checked={consents[i]}
                    onChange={(e) =>
                      setConsents((c) =>
                        c.map((v, j) => (i === j ? e.target.checked : v)),
                      )
                    }
                  />
                }
              />
            ))}
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button disabled={busy} onClick={onClose}>
          {t("Cancel")}
        </Button>
        {review ? (
          <>
            <Button disabled={busy} onClick={() => setReview(false)}>
              {t("Edit details")}
            </Button>
            <Button variant="contained" disabled={busy} onClick={save}>
              {t(busy ? "Saving…" : "Submit application")}
            </Button>
          </>
        ) : (
          <Button form="paper-entry" type="submit" variant="contained">
            {t("Review application")}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
