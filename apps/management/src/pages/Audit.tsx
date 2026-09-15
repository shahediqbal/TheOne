import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  Box,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
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
import { Empty, ErrorBox, Heading, Loading, Pager } from "../ui";
import type { Audit as Event, Page } from "../types";
export default function Audit() {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<Record<string, string>>({});
  const [detail, setDetail] = useState<Event | null>(null);
  const q = useQuery({
    queryKey: ["audit", page, filters],
    queryFn: () =>
      api<Page<Event>>(
        "/admin/audit?" +
          new URLSearchParams({
            page: String(page),
            pageSize: "20",
            ...filters,
          }),
      ),
  });
  return (
    <>
      <Heading
        title="Audit history"
        subtitle="A record of account access and administrative changes."
        action={<Button onClick={() => q.refetch()}>{t("Refresh")}</Button>}
      />
      <Paper variant="outlined">
        <Box
          component="form"
          onSubmit={(e) => {
            e.preventDefault();
            const data = Object.fromEntries(
              new FormData(e.currentTarget),
            ) as Record<string, string>;
            setFilters(
              Object.fromEntries(
                Object.entries(data)
                  .filter(([, v]) => v)
                  .map(([k, v]) => [
                    k,
                    k === "from" || k === "to" ? new Date(v).toISOString() : v,
                  ]),
              ),
            );
            setPage(1);
          }}
          sx={{
            p: 2.5,
          }}
        >
          <Stack
            direction={{
              xs: "column",
              lg: "row",
            }}
            sx={{
              gap: 2,
            }}
          >
            <TextField size="small" name="action" label={t("Action")} />
            <TextField size="small" name="actorId" label={t("Actor ID")} />
            <TextField
              size="small"
              name="from"
              label={t("From")}
              type="datetime-local"
              slotProps={{
                inputLabel: {
                  shrink: true,
                },
              }}
            />
            <TextField
              size="small"
              name="to"
              label={t("To")}
              type="datetime-local"
              slotProps={{
                inputLabel: {
                  shrink: true,
                },
              }}
            />
            <Button
              type="submit"
              variant="contained"
              sx={{
                minWidth: 140,
              }}
            >
              {t("Apply filters")}
            </Button>
          </Stack>
        </Box>
        {q.isFetching && <Loading />}
        <ErrorBox error={q.error} />
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                {["Time", "Action", "Actor ID", "Target", ""].map((x, i) => (
                  <TableCell key={i}>{t(x)}</TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {q.data?.items.map((e) => (
                <TableRow key={e.id}>
                  <TableCell
                    sx={{
                      whiteSpace: "nowrap",
                    }}
                  >
                    {new Date(e.createdAtUtc).toLocaleString()}
                  </TableCell>
                  <TableCell>{e.action}</TableCell>
                  <TableCell
                    sx={{
                      maxWidth: 200,
                      overflowWrap: "anywhere",
                    }}
                  >
                    {e.actorId || "System / anonymous"}
                  </TableCell>
                  <TableCell
                    sx={{
                      maxWidth: 200,
                      overflowWrap: "anywhere",
                    }}
                  >
                    {e.target}
                  </TableCell>
                  <TableCell>
                    <Button onClick={() => setDetail(e)}>{t("Details")}</Button>
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
      {detail && (
        <Dialog open onClose={() => setDetail(null)} fullWidth maxWidth="sm">
          <DialogTitle>{detail.action}</DialogTitle>
          <DialogContent>
            <Box component="pre">
              {(() => {
                try {
                  return JSON.stringify(JSON.parse(detail.details), null, 2);
                } catch {
                  return detail.details;
                }
              })()}
            </Box>
            <Button onClick={() => setDetail(null)}>{t("Close")}</Button>
          </DialogContent>
        </Dialog>
      )}
    </>
  );
}
