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
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
} from "@mui/material";
import { useTranslation } from "react-i18next";
import { api } from "../api";
import { Confirm, Empty, ErrorBox, Heading, Loading, Form } from "../ui";
import { useAuth } from "../auth";
import type { Role } from "../types";
export default function Roles() {
  const { t } = useTranslation();
  const cache = useQueryClient();
  const auth = useAuth();
  const [edit, setEdit] = useState<Role | null | undefined>();
  const [remove, setRemove] = useState<Role | null>(null);
  const [name, setName] = useState("");
  const [grants, setGrants] = useState<string[]>([]);
  const q = useQuery({
    queryKey: ["roles"],
    queryFn: () => api<Role[]>("/admin/roles"),
  });
  const permissions = useQuery({
    queryKey: ["catalog"],
    queryFn: () => api<string[]>("/admin/permissions"),
  });
  const start = (r: Role | null) => {
    setEdit(r);
    setName(r?.name || "");
    setGrants(r?.permissions || []);
  };
  const update = async () => {
    await cache.invalidateQueries({
      queryKey: ["roles"],
    });
    await auth.reload();
  };
  return (
    <>
      <Heading
        title="Roles and permissions"
        subtitle="Choose what each role can do."
        action={
          <Button variant="contained" onClick={() => start(null)}>
            {t("New role")}
          </Button>
        }
      />
      <Paper variant="outlined">
        {q.isFetching && <Loading />}
        <ErrorBox error={q.error || permissions.error} />
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>{t("Role")}</TableCell>
                <TableCell>{t("Permissions")}</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {q.data?.map((r) => (
                <TableRow key={r.id}>
                  <TableCell>
                    {r.name}
                    {r.isSystem && (
                      <Chip
                        size="small"
                        label="System"
                        sx={{
                          ml: 1,
                        }}
                      />
                    )}
                  </TableCell>
                  <TableCell>
                    <Stack
                      direction="row"
                      sx={{
                        flexWrap: "wrap",
                        gap: 1,
                      }}
                    >
                      {r.permissions.map((p) => (
                        <Chip
                          key={p}
                          label={p}
                          size="small"
                          variant="outlined"
                        />
                      ))}
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Button
                      disabled={r.isSystem && r.name !== "Admin"}
                      onClick={() => start(r)}
                    >
                      {t("Edit")}
                    </Button>
                    {!r.isSystem && (
                      <Button color="error" onClick={() => setRemove(r)}>
                        {t("Delete")}
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
        {q.data?.length === 0 && <Empty />}
      </Paper>
      {edit !== undefined && (
        <Dialog open onClose={() => setEdit(undefined)} fullWidth maxWidth="sm">
          <DialogTitle>{t(edit ? "Edit" : "New role")}</DialogTitle>
          <DialogContent>
            <Form
              fields={[]}
              onSubmit={async () => {
                await api(
                  "/admin/roles" + (edit ? "/" + edit.id : ""),
                  edit ? "PUT" : "POST",
                  {
                    name,
                    permissions: grants,
                  },
                );
                await update();
                setEdit(undefined);
              }}
            >
              <TextField
                label={t("Name")}
                required
                value={name}
                disabled={edit?.isSystem}
                onChange={(e) => setName(e.target.value)}
              />
              <Box>
                {permissions.data?.map((p) => (
                  <FormControlLabel
                    key={p}
                    control={
                      <Checkbox
                        checked={grants.includes(p)}
                        onChange={(_, on) =>
                          setGrants(
                            on ? [...grants, p] : grants.filter((x) => x !== p),
                          )
                        }
                      />
                    }
                    label={p}
                  />
                ))}
              </Box>
            </Form>
            <Button
              sx={{
                mt: 2,
              }}
              onClick={() => setEdit(undefined)}
            >
              {t("Cancel")}
            </Button>
          </DialogContent>
        </Dialog>
      )}
      {remove && (
        <Confirm
          title="Delete"
          body={
            "Delete role “" +
            remove.name +
            "”? Assigned roles cannot be deleted."
          }
          onClose={() => setRemove(null)}
          onConfirm={async () => {
            await api("/admin/roles/" + remove.id, "DELETE");
            await update();
          }}
        />
      )}
    </>
  );
}
