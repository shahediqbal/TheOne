import { useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useTranslation } from "react-i18next";
import { api } from "../api";
import { useAuth } from "../auth";
import { Empty, ErrorBox, Heading, Loading, Pager } from "../ui";
import {
  type Detail,
  type Queue,
  statuses,
  paymentTypes,
  textFields,
  enumFields,
  helpOptions,
} from "../membership/model";
import PrivatePhoto from "../membership/PrivatePhoto";
import OperatorEntry from "../membership/OperatorEntry";
export default function Membership() {
  const { reference } = useParams();
  const navigate = useNavigate();
  const cache = useQueryClient();
  const { t, i18n } = useTranslation();
  const { permissions } = useAuth();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [draftSearch, setDraftSearch] = useState("");
  const [status, setStatus] = useState("1");
  const [entry, setEntry] = useState(false);
  const [action, setAction] = useState<
    "verify" | "approve" | "reject" | "contributions" | null
  >(null);
  const [reason, setReason] = useState("");
  const [amount, setAmount] = useState("");
  const [kind, setKind] = useState(0);
  const [method, setMethod] = useState(0);
  const [transaction, setTransaction] = useState("");
  const [note, setNote] = useState("");
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false);
  const inFlight = useRef(false);
  const [error, setError] = useState<unknown>();
  const [notice, setNotice] = useState("");
  const [revealNid, setRevealNid] = useState(false);
  const path = "/admin/membership/applications";
  const q = useQuery({
    queryKey: ["membership", "queue", page, search, status],
    enabled: !reference,
    queryFn: () =>
      api<Queue>(
        path +
          "?" +
          new URLSearchParams({
            page: String(page),
            pageSize: "20",
            search,
            ...(status === ""
              ? {}
              : {
                  status,
                }),
          }),
      ),
  });
  const detail = useQuery({
    queryKey: ["membership", "detail", reference],
    enabled: !!reference,
    queryFn: () => api<Detail>(path + "/" + encodeURIComponent(reference!)),
  });
  const data = detail.data;
  const a = data?.application;
  const can = (p: string) => permissions.includes("membership." + p);
  const date = (value: string | null) =>
    value
      ? new Date(value).toLocaleString(
          i18n.language === "bn" ? "bn-BD" : "en-GB",
        )
      : "—";
  const money = (value: number) =>
    new Intl.NumberFormat(i18n.language === "bn" ? "bn-BD" : "en-BD", {
      style: "currency",
      currency: "BDT",
    }).format(value);
  const feePaid =
    data?.member?.contributions.some((p) => p.type === 0 && p.amount >= 100) ??
    false;
  function openAction(value: typeof action) {
    setAction(value);
    setError(undefined);
    setReason("");
    setAmount("");
    setTransaction("");
    setNote("");
    setConfirmed(false);
    setKind(0);
    setMethod(0);
  }
  async function commit() {
    if (inFlight.current || !reference || !action) return;
    inFlight.current = true;
    setBusy(true);
    setError(undefined);
    try {
      await api(
        path + "/" + encodeURIComponent(reference) + "/" + action,
        "POST",
        action === "reject"
          ? {
              reason,
            }
          : action === "contributions"
            ? {
                type: kind,
                amount: Number(amount),
                method,
                transactionReference: method === 0 ? transaction.trim() : null,
                note: note.trim() || null,
              }
            : {},
      );
      setAction(null);
      setNotice(t("Membership updated"));
      await cache.invalidateQueries({
        queryKey: ["membership"],
      });
    } catch (e) {
      setError(e);
    } finally {
      inFlight.current = false;
      setBusy(false);
    }
  }
  const valid =
    action === "reject"
      ? reason.trim().length > 0
      : action === "contributions"
        ? confirmed &&
          Number.isFinite(Number(amount)) &&
          Number(amount) > 0 &&
          (method !== 0 || !!transaction.trim())
        : confirmed;
  return (
    <>
      <Heading
        title={reference ? "Application details" : "Membership"}
        subtitle={reference || "Review applications and record contributions."}
        action={
          reference ? (
            <Button component={Link} to="/membership">
              {t("Back to applications")}
            </Button>
          ) : can("enter") ? (
            <Button variant="contained" onClick={() => setEntry(true)}>
              {t("Enter paper application")}
            </Button>
          ) : undefined
        }
      />
      {notice && (
        <Alert
          sx={{
            mb: 2,
          }}
          onClose={() => setNotice("")}
        >
          {notice}
        </Alert>
      )}
      {!reference ? (
        <Paper
          variant="outlined"
          sx={{
            p: {
              xs: 2,
              md: 3,
            },
          }}
        >
          <Box
            component="form"
            onSubmit={(e) => {
              e.preventDefault();
              setSearch(draftSearch);
              setPage(1);
            }}
          >
            <Stack
              direction={{
                xs: "column",
                sm: "row",
              }}
              sx={{
                gap: 2,
                mb: 3,
              }}
            >
              <TextField
                label={t("Search name, phone or reference")}
                value={draftSearch}
                onChange={(e) => setDraftSearch(e.target.value)}
                slotProps={{
                  htmlInput: {
                    maxLength: 200,
                  },
                }}
                fullWidth
              />
              <TextField
                select
                label={t("Status")}
                value={status}
                onChange={(e) => {
                  setStatus(e.target.value);
                  setPage(1);
                }}
                sx={{
                  minWidth: 160,
                }}
              >
                <MenuItem value="">{t("All")}</MenuItem>
                {statuses.map((v, i) => (
                  <MenuItem key={v} value={String(i)}>
                    {t(v)}
                  </MenuItem>
                ))}
              </TextField>
              <Button type="submit" variant="outlined">
                {t("Search")}
              </Button>
            </Stack>
          </Box>
          {q.isFetching && <Loading />}
          <ErrorBox error={q.error} />
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  {["Applicant", "Reference", "Status", ""].map((x, i) => (
                    <TableCell key={i}>{t(x)}</TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {q.data?.items.map((item) => (
                  <TableRow key={item.referenceCode} hover>
                    <TableCell>
                      <Typography
                        sx={{
                          fontWeight: 600,
                        }}
                      >
                        {i18n.language === "bn"
                          ? item.fullNameBn || item.fullNameEn
                          : item.fullNameEn || item.fullNameBn}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {item.contactNumber}
                      </Typography>
                    </TableCell>
                    <TableCell>{item.referenceCode}</TableCell>
                    <TableCell>
                      <Chip
                        size="small"
                        variant="outlined"
                        label={t(statuses[item.status] || "Unknown")}
                      />
                    </TableCell>
                    <TableCell>
                      <Button
                        component={Link}
                        to={
                          "/membership/" +
                          encodeURIComponent(item.referenceCode)
                        }
                      >
                        {t("Details")}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          {q.data?.items.length === 0 && <Empty />}
          <Pager page={page} total={q.data?.totalCount || 0} onPage={setPage} />
        </Paper>
      ) : (
        <>
          {detail.isFetching && <Loading />}
          <ErrorBox error={detail.error} />
          {data && a && (
            <Stack
              sx={{
                gap: 3,
              }}
            >
              <Paper
                variant="outlined"
                sx={{
                  p: 3,
                }}
              >
                <Stack
                  direction={{
                    xs: "column",
                    sm: "row",
                  }}
                  sx={{
                    gap: 2,
                    justifyContent: "space-between",
                  }}
                >
                  <Box>
                    <Typography variant="h5">
                      {a.fullNameBn || a.fullNameEn}
                    </Typography>
                    <Typography color="text.secondary">
                      {a.fullNameEn}
                    </Typography>
                    <Typography
                      sx={{
                        mt: 1,
                      }}
                    >
                      {data.member?.membershipNumber || a.referenceCode}
                    </Typography>
                  </Box>
                  <Chip
                    label={t(statuses[a.status] || "Unknown")}
                    color={
                      a.status === 3
                        ? "success"
                        : a.status === 5
                          ? "error"
                          : "default"
                    }
                  />
                </Stack>
                <Stack
                  direction="row"
                  sx={{
                    flexWrap: "wrap",
                    gap: 1,
                    mt: 3,
                  }}
                >
                  {can("review") && a.status === 1 && (
                    <Button
                      variant="outlined"
                      onClick={() => openAction("verify")}
                    >
                      {t("Verify application")}
                    </Button>
                  )}
                  {can("approve") && a.status === 2 && (
                    <Button
                      variant="contained"
                      disabled={!feePaid}
                      onClick={() => openAction("approve")}
                    >
                      {t("Approve membership")}
                    </Button>
                  )}
                  {can("approve") && [1, 2].includes(a.status) && (
                    <Button color="error" onClick={() => openAction("reject")}>
                      {t("Reject application")}
                    </Button>
                  )}
                  {can("contribute") && data.member && (
                    <Button
                      variant="outlined"
                      onClick={() => openAction("contributions")}
                    >
                      {t("Record payment")}
                    </Button>
                  )}
                </Stack>
                {a.status === 2 && !feePaid && (
                  <Alert
                    severity="info"
                    sx={{
                      mt: 2,
                    }}
                  >
                    {t("Record the ৳100 membership fee before approval.")}
                  </Alert>
                )}
                {data.rejectionReason && (
                  <Alert
                    severity="warning"
                    sx={{
                      mt: 2,
                    }}
                  >
                    {data.rejectionReason}
                  </Alert>
                )}
              </Paper>
              <Paper
                variant="outlined"
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
                  {t("Application information")}
                </Typography>
                <Box
                  sx={{
                    display: "grid",
                    gridTemplateColumns: {
                      xs: "1fr",
                      md: "1fr 1fr",
                    },
                    gap: 2.5,
                  }}
                >
                  {textFields.map(([key, label]) => (
                    <Box key={key}>
                      <Typography variant="body2" color="text.secondary">
                        {t(label)}
                      </Typography>
                      {key === "nidNumber" && a[key] && !revealNid ? (
                        <Button onClick={() => setRevealNid(true)}>
                          {t("Show NID")}
                        </Button>
                      ) : (
                        <Typography
                          sx={{
                            whiteSpace: "pre-wrap",
                            overflowWrap: "anywhere",
                          }}
                        >
                          {String(a[key] ?? "") || "—"}
                        </Typography>
                      )}
                    </Box>
                  ))}
                  {enumFields.map(([key, label, options]) => (
                    <Box key={key}>
                      <Typography variant="body2" color="text.secondary">
                        {t(label)}
                      </Typography>
                      <Typography>
                        {a[key] === null
                          ? "—"
                          : t(options[Number(a[key])] || "Unspecified")}
                      </Typography>
                    </Box>
                  ))}
                  {[
                    ["age", "Age"],
                    ["committedSinceYear", "Committed since year"],
                  ].map(([key, label]) => (
                    <Box key={key}>
                      <Typography variant="body2" color="text.secondary">
                        {t(label)}
                      </Typography>
                      <Typography>{String(a[key] ?? "—")}</Typography>
                    </Box>
                  ))}
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      {t("Areas of help")}
                    </Typography>
                    <Typography>
                      {helpOptions
                        .filter(
                          (_, i) => (Number(a.helpCategories) & (1 << i)) !== 0,
                        )
                        .map((v) => t(v))
                        .join(", ") || "—"}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="body2" color="text.secondary">
                      {t("Code of conduct accepted")}
                    </Typography>
                    <Typography>
                      {t(a.codeOfConductAccepted ? "Yes" : "No")}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography>
                      {t("Declaration accepted")}:{" "}
                      {t(a.declarationAccepted ? "Yes" : "No")}
                    </Typography>
                    <Typography>
                      {t("Oath accepted")}: {t(a.oathAccepted ? "Yes" : "No")}
                    </Typography>
                  </Box>
                  {a.photoUrl === "uploaded" && (
                    <PrivatePhoto reference={a.referenceCode} />
                  )}
                </Box>
              </Paper>
              <Paper
                variant="outlined"
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
                  {t("Decision history")}
                </Typography>
                <Typography>
                  {t("Entry channel")}:{" "}
                  {t(
                    data.entryChannel === 1
                      ? "Staff entry"
                      : "Public application",
                  )}
                </Typography>
                {data.enteredByStaffId && (
                  <Typography variant="body2">
                    {t("Staff ID")}: {data.enteredByStaffId}
                  </Typography>
                )}
                {[
                  ["Submitted", a.submittedAtUtc, null],
                  ["Verified", data.verifiedAtUtc, data.verifiedBy],
                  ["Approved", data.approvedAtUtc, data.approvedBy],
                  ["Rejected", data.rejectedAtUtc, data.rejectedBy],
                ].map(
                  ([label, time, actor]) =>
                    time && (
                      <Box
                        key={label}
                        sx={{
                          mt: 2,
                        }}
                      >
                        <Typography>
                          {t(label!)} · {date(time)}
                        </Typography>
                        {actor && (
                          <Typography
                            variant="body2"
                            sx={{
                              overflowWrap: "anywhere",
                            }}
                          >
                            {t("Staff ID")}: {actor}
                          </Typography>
                        )}
                      </Box>
                    ),
                )}
              </Paper>
              <Paper
                variant="outlined"
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
                  {t("Payment history")}
                </Typography>
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        {[
                          "Date",
                          "Type",
                          "Amount",
                          "Method",
                          "Transaction reference",
                          "Note",
                        ].map((v) => (
                          <TableCell key={v}>{t(v)}</TableCell>
                        ))}
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {data.member?.contributions.map((p) => (
                        <TableRow key={p.id}>
                          <TableCell>{date(p.recordedAtUtc)}</TableCell>
                          <TableCell>{t(paymentTypes[p.type])}</TableCell>
                          <TableCell>{money(p.amount)}</TableCell>
                          <TableCell>
                            {p.method === 0 ? "bKash" : t("Cash at office")}
                          </TableCell>
                          <TableCell>{p.transactionReference || "—"}</TableCell>
                          <TableCell
                            sx={{
                              overflowWrap: "anywhere",
                            }}
                          >
                            {p.note || "—"}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
                {!data.member?.contributions.length && <Empty />}
              </Paper>
            </Stack>
          )}
        </>
      )}
      {entry && (
        <OperatorEntry
          onClose={() => setEntry(false)}
          onSaved={async (result) => {
            setEntry(false);
            await cache.invalidateQueries({
              queryKey: ["membership"],
            });
            navigate(
              "/membership/" +
                encodeURIComponent(result.application.referenceCode),
            );
          }}
        />
      )}
      <Dialog
        open={!!action}
        onClose={() => !busy && setAction(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>
          {t(
            action === "contributions"
              ? "Record payment"
              : action === "approve"
                ? "Approve membership"
                : action === "reject"
                  ? "Reject application"
                  : "Verify application",
          )}
        </DialogTitle>
        <DialogContent>
          <Stack
            sx={{
              gap: 2,
              pt: 1,
            }}
          >
            <ErrorBox error={error} />
            <Typography>
              {a?.fullNameBn || a?.fullNameEn} · {reference}
            </Typography>
            {action === "reject" ? (
              <TextField
                autoFocus
                label={t("Reason for rejection")}
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                multiline
                minRows={3}
                required
                slotProps={{
                  htmlInput: {
                    maxLength: 1000,
                  },
                }}
              />
            ) : action === "contributions" ? (
              <>
                <TextField
                  select
                  label={t("Payment type")}
                  value={kind}
                  onChange={(e) => setKind(Number(e.target.value))}
                >
                  {paymentTypes.map((v, i) => (
                    <MenuItem key={v} value={i}>
                      {t(v)}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField
                  label={t("Amount (BDT)")}
                  type="number"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  required
                  slotProps={{
                    htmlInput: {
                      min: 0.01,
                      max: 99999999.99,
                      step: 0.01,
                    },
                  }}
                />
                <TextField
                  select
                  label={t("Method")}
                  value={method}
                  onChange={(e) => setMethod(Number(e.target.value))}
                >
                  <MenuItem value={0}>bKash</MenuItem>
                  <MenuItem value={1}>{t("Cash at office")}</MenuItem>
                </TextField>
                {method === 0 && (
                  <TextField
                    label={t("Transaction reference")}
                    value={transaction}
                    onChange={(e) => setTransaction(e.target.value)}
                    required
                    slotProps={{
                      htmlInput: {
                        maxLength: 100,
                      },
                    }}
                  />
                )}
                <TextField
                  label={t("Note")}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  multiline
                  slotProps={{
                    htmlInput: {
                      maxLength: 500,
                    },
                  }}
                />
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={confirmed}
                      onChange={(e) => setConfirmed(e.target.checked)}
                    />
                  }
                  label={t("I have verified receipt of this payment.")}
                />
                <Alert severity="info">
                  {t(
                    "If a request fails, check payment history before retrying a cash payment.",
                  )}
                </Alert>
              </>
            ) : (
              <FormControlLabel
                control={
                  <Checkbox
                    checked={confirmed}
                    onChange={(e) => setConfirmed(e.target.checked)}
                  />
                }
                label={t(
                  action === "approve"
                    ? "I approve this membership application."
                    : "I have checked the submitted details.",
                )}
              />
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button disabled={busy} onClick={() => setAction(null)}>
            {t("Cancel")}
          </Button>
          <Button
            variant="contained"
            disabled={busy || !valid}
            onClick={commit}
          >
            {t(busy ? "Saving…" : "Confirm")}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
