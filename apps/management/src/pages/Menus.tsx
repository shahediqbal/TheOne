import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
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
import { Confirm, ErrorBox, Heading, Loading, Form } from "../ui";
import type { Menu, Role } from "../types";
const blank: Menu = {
  id: "",
  labelEn: "",
  labelBn: "",
  route: "",
  icon: "",
  parentId: null,
  sortOrder: 10,
  enabled: true,
  requiredPermission: null,
  roleIds: [],
};
export default function Menus() {
  const { t } = useTranslation();
  const cache = useQueryClient();
  const auth = useAuth();
  const [edit, setEdit] = useState<Menu | null>(null);
  const [remove, setRemove] = useState<Menu | null>(null);
  const q = useQuery({
    queryKey: ["menus"],
    queryFn: () => api<Menu[]>("/admin/menus"),
  });
  const roles = useQuery({
    queryKey: ["roles"],
    queryFn: () => api<Role[]>("/admin/roles"),
  });
  const permissions = useQuery({
    queryKey: ["catalog"],
    queryFn: () => api<string[]>("/admin/permissions"),
  });
  const update = async () => {
    await cache.invalidateQueries({
      queryKey: ["menus"],
    });
    await auth.reload();
  };
  const field = (key: keyof Menu, value: unknown) =>
    setEdit((x) =>
      x
        ? {
            ...x,
            [key]: value,
          }
        : x,
    );
  return (
    <>
      <Heading
        title="Menu management"
        subtitle="Organize navigation and assign visibility."
        action={
          <Button
            variant="contained"
            onClick={() =>
              setEdit({
                ...blank,
              })
            }
          >
            {t("New menu")}
          </Button>
        }
      />
      <Paper variant="outlined">
        {q.isFetching && <Loading />}
        <ErrorBox error={q.error || roles.error || permissions.error} />
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                {["Name", "Parent menu", "Route", "Status", ""].map((h, i) => (
                  <TableCell key={i}>{t(h)}</TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {q.data?.map((m) => (
                <TableRow key={m.id}>
                  <TableCell>
                    <Typography
                      sx={{
                        fontWeight: 600,
                      }}
                    >
                      {m.labelEn}
                    </Typography>
                    <Typography
                      variant="body2"
                      sx={{
                        color: "text.secondary",
                      }}
                    >
                      {m.labelBn}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    {q.data?.find((p) => p.id === m.parentId)?.labelEn || "—"}
                  </TableCell>
                  <TableCell>{m.route || "—"}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={m.enabled ? "Enabled" : "Hidden"}
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>
                    <Button
                      onClick={() =>
                        setEdit({
                          ...m,
                        })
                      }
                    >
                      {t("Edit")}
                    </Button>
                    <Button color="error" onClick={() => setRemove(m)}>
                      {t("Delete")}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Paper>
      {edit && (
        <Dialog open onClose={() => setEdit(null)} fullWidth maxWidth="md">
          <DialogTitle>{t(edit.id ? "Edit" : "New menu")}</DialogTitle>
          <DialogContent>
            <Form
              fields={[]}
              onSubmit={async () => {
                await api(
                  "/admin/menus" + (edit.id ? "/" + edit.id : ""),
                  edit.id ? "PUT" : "POST",
                  {
                    ...edit,
                    route: edit.route || null,
                    icon: edit.icon || null,
                    labelBn: edit.labelBn || null,
                  },
                );
                await update();
                setEdit(null);
              }}
            >
              <Stack
                direction={{
                  xs: "column",
                  sm: "row",
                }}
                sx={{
                  gap: 2,
                }}
              >
                <TextField
                  label={t("English label")}
                  required
                  value={edit.labelEn}
                  onChange={(e) => field("labelEn", e.target.value)}
                />
                <TextField
                  label={t("Bangla label")}
                  value={edit.labelBn || ""}
                  onChange={(e) => field("labelBn", e.target.value)}
                />
              </Stack>
              <TextField
                label={t("Route")}
                helperText="Internal path, for example /administration/users. Leave blank for a group."
                value={edit.route || ""}
                onChange={(e) => field("route", e.target.value)}
              />
              <Stack
                direction={{
                  xs: "column",
                  sm: "row",
                }}
                sx={{
                  gap: 2,
                }}
              >
                <TextField
                  select
                  label={t("Parent menu")}
                  value={edit.parentId || ""}
                  onChange={(e) => field("parentId", e.target.value || null)}
                >
                  <MenuItem value="">{t("None")}</MenuItem>
                  {q.data
                    ?.filter((m) => m.id !== edit.id)
                    .map((m) => (
                      <MenuItem key={m.id} value={m.id}>
                        {m.labelEn}
                      </MenuItem>
                    ))}
                </TextField>
                <TextField
                  label={t("Display order")}
                  type="number"
                  value={edit.sortOrder}
                  onChange={(e) => field("sortOrder", Number(e.target.value))}
                />
                <TextField
                  label={t("Icon")}
                  value={edit.icon || ""}
                  onChange={(e) => field("icon", e.target.value)}
                />
              </Stack>
              <TextField
                select
                label={t("Required permission")}
                value={edit.requiredPermission || ""}
                onChange={(e) =>
                  field("requiredPermission", e.target.value || null)
                }
              >
                <MenuItem value="">{t("None")}</MenuItem>
                {permissions.data?.map((p) => (
                  <MenuItem key={p} value={p}>
                    {p}
                  </MenuItem>
                ))}
              </TextField>
              <Box>
                <Typography>{t("Visible to roles")}</Typography>
                {roles.data?.map((r) => (
                  <FormControlLabel
                    key={r.id}
                    label={r.name}
                    control={
                      <Checkbox
                        checked={edit.roleIds.includes(r.id)}
                        onChange={(_, on) =>
                          field(
                            "roleIds",
                            on
                              ? [...edit.roleIds, r.id]
                              : edit.roleIds.filter((x) => x !== r.id),
                          )
                        }
                      />
                    }
                  />
                ))}
              </Box>
              <FormControlLabel
                label={t("Enabled")}
                control={
                  <Checkbox
                    checked={edit.enabled}
                    onChange={(_, on) => field("enabled", on)}
                  />
                }
              />
            </Form>
            <Button
              sx={{
                mt: 2,
              }}
              onClick={() => setEdit(null)}
            >
              {t("Cancel")}
            </Button>
          </DialogContent>
        </Dialog>
      )}
      {remove && (
        <Confirm
          title="Delete"
          body={"Delete “" + remove.labelEn + "”? Remove child menus first."}
          onClose={() => setRemove(null)}
          onConfirm={async () => {
            await api("/admin/menus/" + remove.id, "DELETE");
            await update();
          }}
        />
      )}
    </>
  );
}
