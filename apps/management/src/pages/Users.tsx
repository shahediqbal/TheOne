import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
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
import { Confirm, Empty, ErrorBox, Heading, Loading, Pager } from "../ui";
import type { User, Page, Role } from "../types";
export default function Users() {
  const { t } = useTranslation();
  const auth = useAuth();
  const cache = useQueryClient();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [draft, setDraft] = useState("");
  const [status, setStatus] = useState("");
  const [role, setRole] = useState("");
  const [detail, setDetail] = useState<User | null>(null);
  const [change, setChange] = useState<User | null>(null);
  const [assign, setAssign] = useState("");
  const [error, setError] = useState<unknown>();
  const [busy, setBusy] = useState(false);
  const owner = auth.profile!.roles.includes("SuperAdmin");
  const roles = useQuery({
    queryKey: ["roles"],
    queryFn: () => api<Role[]>("/admin/roles"),
    enabled: owner,
  });
  const q = useQuery({
    queryKey: ["users", page, search, status, role],
    queryFn: () =>
      api<Page<User>>(
        "/admin/users?" +
          new URLSearchParams({
            page: String(page),
            pageSize: "20",
            search,
            ...(status
              ? {
                  isActive: status,
                }
              : {}),
            ...(role
              ? {
                  role,
                }
              : {}),
          }),
      ),
  });
  const refresh = () =>
    cache.invalidateQueries({
      queryKey: ["users"],
    });
  const manage = (u: User) =>
    auth.permissions.includes("users.manage") &&
    u.id !== auth.profile!.userId &&
    !u.roles.includes("SuperAdmin") &&
    (owner || !u.roles.includes("Admin"));
  const assignment = async (roleId: string, remove = false) => {
    setBusy(true);
    setError(undefined);
    try {
      await api(
        "/admin/users/" + detail!.id + "/roles/" + roleId,
        remove ? "DELETE" : "PUT",
      );
      setDetail(await api<User>("/admin/users/" + detail!.id));
      setAssign("");
      await refresh();
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  };
  return (
    <>
      <Heading
        title="Users"
        subtitle="Find accounts and manage their access."
        action={<Button onClick={() => q.refetch()}>{t("Refresh")}</Button>}
      />
      <Paper variant="outlined">
        <Box
          component="form"
          onSubmit={(e) => {
            e.preventDefault();
            setSearch(draft);
            setPage(1);
          }}
          sx={{
            p: 2.5,
          }}
        >
          <Stack
            direction={{
              xs: "column",
              md: "row",
            }}
            sx={{
              gap: 2,
            }}
          >
            <TextField
              size="small"
              label={t("Search")}
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              placeholder="Name, email or mobile"
            />
            <TextField
              select
              size="small"
              label={t("Status")}
              value={status}
              onChange={(e) => {
                setStatus(e.target.value);
                setPage(1);
              }}
              sx={{
                minWidth: 150,
              }}
            >
              {[
                ["", "All"],
                ["true", "Active"],
                ["false", "Inactive"],
              ].map(([v, l]) => (
                <MenuItem key={v} value={v}>
                  {t(l)}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              size="small"
              label={t("Role")}
              value={role}
              onChange={(e) => {
                setRole(e.target.value);
                setPage(1);
              }}
            />
            <Button type="submit" variant="contained">
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
                {["Name", "Roles", "Status", ""].map((h, i) => (
                  <TableCell key={i}>{t(h)}</TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {q.data?.items.map((u) => (
                <TableRow key={u.id} hover>
                  <TableCell>
                    <Typography
                      sx={{
                        fontWeight: 600,
                      }}
                    >
                      {u.fullName}
                    </Typography>
                    <Typography
                      variant="body2"
                      sx={{
                        color: "text.secondary",
                      }}
                    >
                      {u.email}
                    </Typography>
                  </TableCell>
                  <TableCell>{u.roles.join(", ")}</TableCell>
                  <TableCell>
                    <Chip
                      label={t(u.isActive ? "Active" : "Inactive")}
                      color={u.isActive ? "success" : "default"}
                      size="small"
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>
                    <Stack direction="row">
                      <Button
                        onClick={async () => {
                          setError(undefined);
                          try {
                            setDetail(await api<User>("/admin/users/" + u.id));
                          } catch (e) {
                            setError(e);
                          }
                        }}
                      >
                        {t("Details")}
                      </Button>
                      {manage(u) && (
                        <Button
                          color={u.isActive ? "error" : "primary"}
                          onClick={() => setChange(u)}
                        >
                          {t(u.isActive ? "Deactivate" : "Activate")}
                        </Button>
                      )}
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
        {q.data?.items.length === 0 && <Empty />}
        <Box
          sx={{
            p: 2,
          }}
        >
          <Pager page={page} total={q.data?.totalCount || 0} onPage={setPage} />
        </Box>
      </Paper>
      <ErrorBox error={!detail ? error : null} />
      {change && (
        <Confirm
          title={change.isActive ? "Deactivate" : "Activate"}
          body={
            change.fullName +
            " — " +
            (change.isActive
              ? "This account will lose access and active sessions will be revoked."
              : "The user must sign in again.")
          }
          onClose={() => setChange(null)}
          onConfirm={async () => {
            await api("/admin/users/" + change.id + "/status", "PATCH", {
              isActive: !change.isActive,
            });
            await refresh();
          }}
        />
      )}
      {detail && (
        <Dialog
          open
          onClose={() => !busy && setDetail(null)}
          fullWidth
          maxWidth="sm"
        >
          <DialogTitle>{t("Account details")}</DialogTitle>
          <DialogContent>
            <Stack spacing={2}>
              <ErrorBox error={error} />
              <Typography variant="h6">{detail.fullName}</Typography>
              <Typography>{detail.email}</Typography>
              <Typography>{detail.mobileNumber}</Typography>
              <Typography variant="body2">ID: {detail.id}</Typography>
              <Typography>
                Mobile verified: {detail.mobileVerified ? "Yes" : "No"} ·
                Authenticator:{" "}
                {detail.authenticatorEnabled ? "Enabled" : "Not enrolled"}
              </Typography>
              <Stack
                sx={{
                  gap: 1,
                }}
              >
                {detail.roles.map((name) => {
                  const r = roles.data?.find((x) => x.name === name);
                  return (
                    <Stack
                      key={name}
                      direction="row"
                      sx={{
                        justifyContent: "space-between",
                      }}
                    >
                      <Chip label={name} />
                      {owner &&
                        detail.id !== auth.profile!.userId &&
                        r &&
                        !["Member", "SuperAdmin"].includes(name) && (
                          <Button
                            disabled={busy}
                            color="error"
                            onClick={() => assignment(r.id, true)}
                          >
                            {t("Remove")}
                          </Button>
                        )}
                    </Stack>
                  );
                })}
              </Stack>
              {owner && detail.id !== auth.profile!.userId && (
                <>
                  <TextField
                    select
                    label={t("Assign role")}
                    value={assign}
                    onChange={(e) => setAssign(e.target.value)}
                  >
                    {roles.data
                      ?.filter((r) => !detail.roles.includes(r.name))
                      .map((r) => (
                        <MenuItem key={r.id} value={r.id}>
                          {r.name}
                        </MenuItem>
                      ))}
                  </TextField>
                  <Button
                    disabled={!assign || busy}
                    variant="outlined"
                    onClick={() => assignment(assign)}
                  >
                    {t("Assign role")}
                  </Button>
                </>
              )}
              <Button onClick={() => setDetail(null)} disabled={busy}>
                {t("Close")}
              </Button>
            </Stack>
          </DialogContent>
        </Dialog>
      )}
    </>
  );
}
