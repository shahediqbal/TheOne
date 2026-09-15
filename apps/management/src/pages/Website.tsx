import { useEffect, useRef, useState } from "react";
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
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useTranslation } from "react-i18next";
import { api, apiForm, apiBlob } from "../api";
import { useAuth } from "../auth";
import { ErrorBox, Heading } from "../ui";
import { states } from "../../../../packages/blog/types";
import BlocksEditor from "../content/BlocksEditor";
import {
  kindBn,
  type Field,
  type Schema,
  type Payload,
  type RecordView,
  type Candidate,
  type Asset,
} from "../content/WebsiteTypes";
type Revision = {
  id: string;
  version: number;
  action: string;
  actorId: string;
  createdAtUtc: string;
};
function AssetPreview({ id }: { id: string }) {
  const [url, setUrl] = useState(""),
    [error, setError] = useState<unknown>();
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
          alt="Uploaded image preview"
          style={{ maxWidth: 200, maxHeight: 160 }}
        />
      ) : (
        <Button
          onClick={async () => {
            try {
              setUrl(
                URL.createObjectURL(
                  await apiBlob("/admin/website/assets/" + id),
                ),
              );
            } catch (e) {
              setError(e);
            }
          }}
        >
          Preview image
        </Button>
      )}
    </Box>
  );
}
export default function Website() {
  const auth = useAuth(),
    { i18n } = useTranslation(),
    bn = i18n.language.startsWith("bn");
  const tr = (en: string, bnText: string) => (bn ? bnText : en);
  const edit = auth.permissions.includes("website.edit"),
    approve =
      auth.permissions.includes("website.approve") &&
      auth.profile?.roles.some((r) => r === "Admin" || r === "SuperAdmin");
  const [schemas, setSchemas] = useState<Schema[]>([]),
    [kind, setKind] = useState("Page"),
    [language, setLanguage] = useState("bn"),
    [page, setPage] = useState(1),
    [search, setSearch] = useState(""),
    [rows, setRows] = useState<RecordView[]>([]),
    [selected, setSelected] = useState<RecordView>(),
    [doc, setDoc] = useState<Payload>(),
    [dirty, setDirty] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<unknown>(),
    [confirm, setConfirm] = useState(""),
    [checked, setChecked] = useState(false),
    [history, setHistory] = useState<Revision[]>([]),
    [assets, setAssets] = useState<Asset[]>([]),
    [staff, setStaff] = useState<Candidate[]>([]),
    [staffId, setStaffId] = useState(""),
    [lookups, setLookups] = useState<Record<string, Candidate[]>>({}),
    [preview, setPreview] = useState<Payload>();
  const pending = useRef(false),
    lookupPages = useRef<Record<string, number>>({});
  const schema = schemas.find((s) => s.kind === kind);
  const base = "/admin/website";
  const reload = async () =>
    setRows(
      await api<RecordView[]>(
        `${base}/records?kind=${kind}&page=${page}&search=${encodeURIComponent(search)}`,
      ),
    );
  useEffect(() => {
    api<Schema[]>(base + "/schema")
      .then(setSchemas)
      .catch(setError);
  }, []);
  useEffect(() => {
    let active = true;
    api<RecordView[]>(
      `${base}/records?kind=${kind}&page=${page}&search=${encodeURIComponent(search)}`,
    )
      .then((r) => {
        if (active) setRows(r);
      })
      .catch((e) => {
        if (active) setError(e);
      });
    return () => {
      active = false;
    };
  }, [kind, page, search]);
  useEffect(() => {
    const warn = (e: BeforeUnloadEvent) => {
      if (dirty) {
        e.preventDefault();
        e.returnValue = "";
      }
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);
  const change = (next: Payload) => {
    setDoc(next);
    setDirty(true);
    setPreview(undefined);
  };
  const changeField = (key: string, value: unknown) =>
    change({ ...doc!, fields: { ...doc!.fields, [key]: value } });
  const accept = (v: RecordView) => {
    setSelected(v);
    setDoc(v.document);
    setDirty(false);
    setPreview(undefined);
    setHistory([]);
  };
  const discard = () =>
    !dirty ||
    window.confirm(
      tr("Discard unsaved changes?", "সংরক্ষণ না করা পরিবর্তন বাদ দেবেন?"),
    );
  async function run(fn: () => Promise<void>) {
    if (pending.current) return;
    pending.current = true;
    setBusy(true);
    setError(undefined);
    try {
      await fn();
    } catch (e) {
      setError(e);
    } finally {
      pending.current = false;
      setBusy(false);
    }
  }
  async function lookup(target: string) {
    const p = (lookupPages.current[target] || 0) + 1;
    const data = await api<Candidate[]>(
      `${base}/lookup?kind=${target}&page=${p}`,
    );
    lookupPages.current[target] = p;
    setLookups((old) => ({
      ...old,
      [target]: [...(old[target] || []), ...data].filter(
        (r, i, a) =>
          a.findIndex((v) => v.id === r.id && v.language === r.language) === i,
      ),
    }));
  }
  async function loadAssets() {
    const p = (lookupPages.current.asset || 0) + 1;
    const data = await api<Asset[]>(base + "/assets?page=" + p);
    lookupPages.current.asset = p;
    setAssets((a) =>
      [...a, ...data].filter(
        (r, i, all) => all.findIndex((v) => v.id === r.id) === i,
      ),
    );
  }
  async function loadStaff() {
    const p = (lookupPages.current.staff || 0) + 1;
    const data = await api<Candidate[]>(base + "/staff?page=" + p);
    lookupPages.current.staff = p;
    setStaff((a) => [...a, ...data]);
  }
  async function action(name: string, revisionId?: string) {
    if (!selected) return;
    accept(
      await api<RecordView>(
        `${base}/records/${selected.id}/actions/${name}`,
        "POST",
        { expectedVersion: selected.version, revisionId },
      ),
    );
    setConfirm("");
    await reload();
  }
  function field(f: Field) {
    const value = doc!.fields[f.key];
    const label = f.label + (f.required ? " *" : "");
    if (f.type === "boolean")
      return (
        <FormControlLabel
          key={f.key}
          label={label}
          control={
            <Checkbox
              checked={value === true}
              onChange={(e) => changeField(f.key, e.target.checked)}
            />
          }
        />
      );
    if (f.type === "links" || f.type === "menu") {
      const items = (value || []) as Record<string, string>[];
      return (
        <Paper key={f.key} variant="outlined" sx={{ p: 2 }}>
          <Typography>{label}</Typography>
          <Stack spacing={2}>
            {items.map((item, i) => (
              <Box
                key={i}
                sx={{
                  display: "grid",
                  gridTemplateColumns: { xs: "1fr", md: "1fr 1fr" },
                  gap: 1,
                }}
              >
                {(f.type === "menu"
                  ? ["key", "label", "href", "parentKey"]
                  : ["label", "href"]
                ).map((key) => (
                  <TextField
                    key={key}
                    label={
                      {
                        key: "Entry key",
                        label: "Link label",
                        href: "Destination (/path or https://...)",
                        parentKey: "Parent entry key (optional)",
                      }[key]
                    }
                    value={item[key] || ""}
                    onChange={(e) =>
                      changeField(
                        f.key,
                        items.map((v, j) =>
                          i === j ? { ...v, [key]: e.target.value } : v,
                        ),
                      )
                    }
                  />
                ))}
                <Button
                  disabled={i === 0}
                  onClick={() => {
                    const copy = [...items];
                    [copy[i - 1], copy[i]] = [copy[i], copy[i - 1]];
                    changeField(f.key, copy);
                  }}
                >
                  ↑
                </Button>
                <Button
                  onClick={() =>
                    changeField(
                      f.key,
                      items.filter((_, j) => i !== j),
                    )
                  }
                >
                  Remove
                </Button>
              </Box>
            ))}
            <Button
              disabled={items.length >= 60}
              onClick={() =>
                changeField(f.key, [
                  ...items,
                  {
                    ...(f.type === "menu"
                      ? {
                          key: "item-" + crypto.randomUUID().slice(0, 8),
                          parentKey: "",
                        }
                      : {}),
                    label: "",
                    href: "",
                  },
                ])
              }
            >
              Add link
            </Button>
          </Stack>
        </Paper>
      );
    }
    if (["reference", "asset", "posts"].includes(f.type)) {
      const target = f.type === "posts" ? "BlogPost" : f.targetKind || "";
      const choices =
        f.type === "asset"
          ? assets.map((a) => ({ id: a.id, title: a.originalName }))
          : lookups[target] || [];
      const unique = choices.filter(
        (v, i, a) => a.findIndex((x) => x.id === v.id) === i,
      );
      const ids = f.type === "posts" ? ((value || []) as string[]) : [];
      return (
        <Stack key={f.key} spacing={1}>
          <TextField
            select
            label={label}
            value={f.type === "posts" ? "" : String(value || "")}
            onChange={(e) =>
              changeField(
                f.key,
                f.type === "posts"
                  ? [...new Set([...ids, e.target.value])]
                  : e.target.value,
              )
            }
          >
            <MenuItem value="">Select</MenuItem>
            {f.type !== "posts" &&
            value &&
            !unique.some((c) => c.id === value) ? (
              <MenuItem value={String(value)}>
                Saved selection: {String(value)}
              </MenuItem>
            ) : null}
            {unique
              .filter((c) => !ids.includes(c.id))
              .map((c) => (
                <MenuItem key={c.id} value={c.id}>
                  {c.title}
                </MenuItem>
              ))}
          </TextField>
          <Button
            onClick={() =>
              void run(f.type === "asset" ? loadAssets : () => lookup(target))
            }
          >
            Load more choices
          </Button>
          {f.type === "asset" && !!value && (
            <AssetPreview key={String(value)} id={String(value)} />
          )}
          {ids.map((id, i) => (
            <Stack
              key={id}
              direction="row"
              spacing={1}
              sx={{ alignItems: "center" }}
            >
              <Typography>
                {i + 1}. {unique.find((c) => c.id === id)?.title || id}
              </Typography>
              <Button
                disabled={i === 0}
                onClick={() => {
                  const copy = [...ids];
                  [copy[i - 1], copy[i]] = [copy[i], copy[i - 1]];
                  changeField(f.key, copy);
                }}
              >
                ↑
              </Button>
              <Button
                onClick={() =>
                  changeField(
                    f.key,
                    ids.filter((x) => x !== id),
                  )
                }
              >
                Remove
              </Button>
            </Stack>
          ))}
        </Stack>
      );
    }
    const numeric = ["integer", "latitude", "longitude"].includes(f.type);
    return (
      <TextField
        key={f.key}
        label={label}
        select={f.type === "select"}
        type={numeric ? "number" : f.type === "email" ? "email" : "text"}
        multiline={f.type === "textarea"}
        minRows={f.type === "textarea" ? 3 : undefined}
        helperText={
          f.type === "datetime"
            ? "Example: 2026-10-01T18:00:00+06:00"
            : undefined
        }
        value={value ?? ""}
        onChange={(e) =>
          changeField(
            f.key,
            e.target.value === ""
              ? undefined
              : numeric
                ? Number(e.target.value)
                : e.target.value,
          )
        }
      >
        {f.type === "select"
          ? [
              <MenuItem value="" key="empty">
                Select
              </MenuItem>,
              ...(f.choices || []).map((c) => (
                <MenuItem key={c} value={c}>
                  {c}
                </MenuItem>
              )),
            ]
          : undefined}
      </TextField>
    );
  }
  return (
    <>
      <Heading
        title={tr("Website content", "ওয়েবসাইটের বিষয়বস্তু")}
        subtitle={tr(
          "Edit, review and approve content for the future public website.",
          "পাবলিক ওয়েবসাইটের বিষয়বস্তু সম্পাদনা, পর্যালোচনা ও অনুমোদন করুন।",
        )}
      />
      <ErrorBox error={error} />
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <TextField
            select
            label={tr("Content type", "বিষয়ের ধরন")}
            value={kind}
            sx={{ minWidth: 220 }}
            onChange={(e) => {
              if (discard()) {
                setKind(e.target.value);
                setPage(1);
                setSelected(undefined);
                setDoc(undefined);
                setDirty(false);
              }
            }}
          >
            {schemas.length ? (
              schemas.map((s) => (
                <MenuItem key={s.kind} value={s.kind}>
                  {bn ? kindBn[s.kind] : s.label}
                </MenuItem>
              ))
            ) : (
              <MenuItem value="Page">Pages</MenuItem>
            )}
          </TextField>
          <TextField
            label={tr("Search titles", "শিরোনাম খুঁজুন")}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <TextField
            select
            label="New record language"
            value={language}
            onChange={(e) => setLanguage(e.target.value)}
          >
            <MenuItem value="bn">বাংলা</MenuItem>
            <MenuItem value="en">English</MenuItem>
          </TextField>
          {edit && (
            <Button
              disabled={busy || (kind === "AuthorProfile" && !staffId)}
              variant="contained"
              onClick={() => {
                if (discard())
                  void run(async () => {
                    accept(
                      await api<RecordView>(base + "/records", "POST", {
                        kind,
                        language,
                        staffId: kind === "AuthorProfile" ? staffId : null,
                      }),
                    );
                    await reload();
                  });
              }}
            >
              {tr("New record", "নতুন বিষয়")}
            </Button>
          )}
        </Stack>
        {kind === "AuthorProfile" && edit && (
          <Stack direction="row" spacing={2}>
            <TextField
              select
              fullWidth
              label="Existing staff identity for a new author"
              value={staffId}
              onChange={(e) => setStaffId(e.target.value)}
            >
              <MenuItem value="">Select</MenuItem>
              {staff.map((u) => (
                <MenuItem key={u.id} value={u.id}>
                  {u.title || u.id}
                </MenuItem>
              ))}
            </TextField>
            <Button disabled={busy} onClick={() => void run(loadStaff)}>
              Load staff
            </Button>
          </Stack>
        )}
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", lg: "280px 1fr" },
            gap: 3,
          }}
        >
          <Paper sx={{ p: 2, alignSelf: "start" }}>
            <Stack spacing={1}>
              {rows.map((r) => (
                <Button
                  key={r.id}
                  disabled={busy}
                  variant={r.id === selected?.id ? "contained" : "text"}
                  sx={{ justifyContent: "start", textAlign: "left" }}
                  onClick={() => {
                    if (discard())
                      void run(async () =>
                        accept(
                          await api<RecordView>(base + "/records/" + r.id),
                        ),
                      );
                  }}
                >
                  {r.document.content.title} · {r.language} · {states[r.status]}
                </Button>
              ))}
              <Stack direction="row">
                <Button
                  disabled={page === 1 || busy}
                  onClick={() => setPage((p) => p - 1)}
                >
                  Previous
                </Button>
                <Button
                  disabled={rows.length < 30 || busy}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </Button>
              </Stack>
            </Stack>
          </Paper>
          {selected && doc ? (
            <Stack spacing={2}>
              <Alert severity="info">
                {states[selected.status]} · {selected.language} · Version{" "}
                {selected.version}
                {selected.publishedRevisionId
                  ? " · An approved revision is public"
                  : ""}
                {dirty ? " · Unsaved changes" : ""}
              </Alert>
              <Box
                component="fieldset"
                disabled={busy || !edit || [1, 2].includes(selected.status)}
                sx={{ border: 0, p: 0, m: 0, minWidth: 0 }}
              >
                <Stack spacing={2}>
                  {(
                    [
                      "title",
                      "slug",
                      "summary",
                      "seoTitle",
                      "seoDescription",
                    ] as const
                  ).map((key, i) => (
                    <TextField
                      key={key}
                      label={
                        [
                          "Title",
                          "Slug",
                          "Summary",
                          "SEO title",
                          "SEO description",
                        ][i]
                      }
                      value={doc.content[key]}
                      multiline={key === "summary" || key === "seoDescription"}
                      onChange={(e) =>
                        change({
                          ...doc,
                          content: { ...doc.content, [key]: e.target.value },
                        })
                      }
                    />
                  ))}
                  {schema?.fields.map(field)}
                  <Typography variant="h6">Body content</Typography>
                  <BlocksEditor
                    blocks={doc.content.blocks}
                    onChange={(blocks) =>
                      change({ ...doc, content: { ...doc.content, blocks } })
                    }
                  />
                  <FormControlLabel
                    label="AI assisted this content or translation"
                    control={
                      <Checkbox
                        checked={doc.content.provenance !== "human"}
                        disabled={
                          selected.document.content.provenance !== "human"
                        }
                        onChange={(e) =>
                          change({
                            ...doc,
                            content: {
                              ...doc.content,
                              provenance: e.target.checked
                                ? "ai_assisted_draft"
                                : "human",
                            },
                          })
                        }
                      />
                    }
                  />
                </Stack>
              </Box>
              <Stack
                direction="row"
                spacing={1}
                useFlexGap
                sx={{ flexWrap: "wrap" }}
              >
                <Button
                  variant="contained"
                  disabled={
                    busy || !edit || !dirty || [1, 2].includes(selected.status)
                  }
                  onClick={() =>
                    void run(async () => {
                      accept(
                        await api<RecordView>(
                          `${base}/records/${selected.id}`,
                          "PUT",
                          { expectedVersion: selected.version, document: doc },
                        ),
                      );
                      await reload();
                    })
                  }
                >
                  Save draft
                </Button>
                <Button
                  disabled={busy || dirty}
                  onClick={() =>
                    void run(async () => {
                      const p = await api<{ token: string }>(
                        `${base}/records/${selected.id}/preview`,
                        "POST",
                        {},
                      );
                      setPreview(
                        await api<Payload>("/website/preview", "POST", {
                          token: p.token,
                        }),
                      );
                    })
                  }
                >
                  Saved preview
                </Button>
                {edit && selected.status === 0 && (
                  <Button
                    disabled={
                      busy ||
                      dirty ||
                      doc.content.provenance === "ai_assisted_draft"
                    }
                    onClick={() => void run(() => action("review"))}
                  >
                    Send for review
                  </Button>
                )}
                {edit && [1, 2].includes(selected.status) && (
                  <Button
                    disabled={busy}
                    onClick={() => void run(() => action("return"))}
                  >
                    Return to draft
                  </Button>
                )}
                {approve &&
                  selected.status === 0 &&
                  doc.content.provenance === "ai_assisted_draft" && (
                    <Button
                      disabled={busy || dirty}
                      onClick={() => {
                        setConfirm("review-ai");
                        setChecked(false);
                      }}
                    >
                      Confirm human review
                    </Button>
                  )}
                {approve && selected.status === 1 && (
                  <Button
                    disabled={busy}
                    onClick={() => {
                      setConfirm("approve");
                      setChecked(false);
                    }}
                  >
                    Approve
                  </Button>
                )}
                {approve && selected.status === 2 && (
                  <Button
                    disabled={busy}
                    onClick={() => {
                      setConfirm("publish");
                      setChecked(false);
                    }}
                  >
                    Publish
                  </Button>
                )}
                {approve && selected.status !== 4 && (
                  <Button
                    disabled={busy || dirty}
                    onClick={() => {
                      setConfirm("archive");
                      setChecked(false);
                    }}
                  >
                    Archive
                  </Button>
                )}
                <Button
                  disabled={busy}
                  onClick={() =>
                    void run(async () =>
                      setHistory(
                        await api<Revision[]>(
                          `${base}/records/${selected.id}/revisions`,
                        ),
                      ),
                    )
                  }
                >
                  History
                </Button>
                {edit && (
                  <Button
                    disabled={busy || dirty}
                    onClick={() =>
                      void run(async () => {
                        accept(
                          await api<RecordView>(base + "/records", "POST", {
                            kind: selected.kind,
                            language: selected.language === "bn" ? "en" : "bn",
                            canonicalId: selected.canonicalId,
                          }),
                        );
                        await reload();
                      })
                    }
                  >
                    Add translation
                  </Button>
                )}
              </Stack>
              {selected.approvedBy && (
                <Typography>
                  Approved by {selected.approvedBy} · {selected.approvedAtUtc}
                </Typography>
              )}
              {history.map((r) => (
                <Paper
                  key={r.id}
                  sx={{ p: 2, display: "flex", gap: 2, alignItems: "center" }}
                >
                  <Typography sx={{ flex: 1 }}>
                    Version {r.version} · {r.action} · {r.createdAtUtc} ·{" "}
                    {r.actorId}
                  </Typography>
                  {edit && (
                    <Button
                      disabled={busy || dirty}
                      onClick={() => {
                        setConfirm("restore:" + r.id);
                        setChecked(false);
                      }}
                    >
                      Restore as draft
                    </Button>
                  )}
                </Paper>
              ))}
            </Stack>
          ) : (
            <Alert severity="info">
              Select content or create a new record.
            </Alert>
          )}
        </Box>
        <Paper sx={{ p: 3 }}>
          <Typography variant="h6">Shared images</Typography>
          <Typography>
            JPEG/PNG, up to 10 MB. Images are private until referenced by
            approved published CMS content.
          </Typography>
          {edit && (
            <Button component="label" disabled={busy}>
              Upload image
              <input
                hidden
                type="file"
                accept="image/jpeg,image/png"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (!file) return;
                  void run(async () => {
                    if (file.size > 10 * 1024 * 1024)
                      throw new Error("Maximum image size is 10 MB.");
                    const form = new FormData();
                    form.append("image", file);
                    const asset = await apiForm<Asset>(base + "/assets", form);
                    setAssets((a) => [asset, ...a]);
                  });
                  e.target.value = "";
                }}
              />
            </Button>
          )}
          <Button disabled={busy} onClick={() => void run(loadAssets)}>
            Load more images
          </Button>
          {assets.map((a) => (
            <Stack
              key={a.id}
              direction="row"
              spacing={2}
              sx={{ alignItems: "center", py: 1 }}
            >
              <Typography>
                {a.originalName} · {Math.ceil(a.bytes / 1024)} KB
              </Typography>
              <AssetPreview id={a.id} />
            </Stack>
          ))}
        </Paper>
      </Stack>
      <Dialog open={!!confirm} onClose={() => !busy && setConfirm("")}>
        <DialogTitle>Confirm {confirm.split(":")[0]}</DialogTitle>
        <DialogContent>
          <ErrorBox error={error} />
          <Typography>
            {confirm.startsWith("restore")
              ? "This restores the selected revision as a draft; the approved public version remains unchanged."
              : confirm === "review-ai"
                ? "Confirm that you have checked the AI-assisted content, translation and sources."
                : confirm === "archive"
                  ? "This removes the translation from public APIs. Published references may prevent archiving."
                  : "Confirm this action for the saved version. Unsaved work is not published."}
          </Typography>
          <FormControlLabel
            label="I confirm this action."
            control={
              <Checkbox
                checked={checked}
                onChange={(e) => setChecked(e.target.checked)}
              />
            }
          />
        </DialogContent>
        <DialogActions>
          <Button disabled={busy} onClick={() => setConfirm("")}>
            Cancel
          </Button>
          <Button
            disabled={!checked || busy}
            onClick={() =>
              void run(() =>
                action(confirm.split(":")[0], confirm.split(":")[1]),
              )
            }
          >
            Confirm
          </Button>
        </DialogActions>
      </Dialog>
      <Dialog
        open={!!preview}
        onClose={() => setPreview(undefined)}
        fullWidth
        maxWidth="md"
      >
        <DialogTitle>Saved content preview</DialogTitle>
        <DialogContent>
          {preview && (
            <Stack spacing={2}>
              <Typography variant="h4">{preview.content.title}</Typography>
              <Typography>{preview.content.summary}</Typography>
              {preview.content.blocks.map((b, i) => (
                <Box key={i}>
                  <Typography
                    variant={b.type === "Heading" ? "h5" : "body1"}
                    sx={{ whiteSpace: "pre-wrap" }}
                  >
                    {b.text}
                  </Typography>
                  {b.citation && <Typography>{b.citation}</Typography>}
                  {b.url && <Typography>{b.url}</Typography>}
                </Box>
              ))}
              {Object.entries(preview.fields).map(([key, value]) => (
                <Box key={key}>
                  <Typography color="text.secondary">
                    {schema?.fields.find((f) => f.key === key)?.label || key}
                  </Typography>
                  <Typography sx={{ whiteSpace: "pre-wrap" }}>
                    {typeof value === "object"
                      ? JSON.stringify(value, null, 2)
                      : String(value)}
                  </Typography>
                </Box>
              ))}
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPreview(undefined)}>Close</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
