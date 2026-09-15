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
import { api } from "../api";
import { useAuth } from "../auth";
import { ErrorBox, Heading } from "../ui";
import {
  blockTypes,
  states,
  type Block,
  type BlogDocument,
  type BlogView,
} from "../../../../packages/blog/types";
type Revision = {
  id: string;
  version: number;
  action: string;
  actorId: string;
  createdAtUtc: string;
};
export default function Blog() {
  const { i18n } = useTranslation();
  const bn = i18n.language.startsWith("bn");
  const text = (a: string, b: string) => (bn ? b : a);
  const auth = useAuth();
  const edit = auth.permissions.includes("blog.edit"),
    admin =
      auth.permissions.includes("blog.approve") &&
      auth.profile?.roles.some((r) => r === "Admin" || r === "SuperAdmin");
  const [rows, setRows] = useState<BlogView[]>([]),
    [page, setPage] = useState(1),
    [selected, setSelected] = useState<BlogView>(),
    [doc, setDoc] = useState<BlogDocument>(),
    [dirty, setDirty] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState<unknown>(),
    [history, setHistory] = useState<Revision[]>([]),
    [preview, setPreview] = useState(""),
    [confirm, setConfirm] = useState(""),
    [confirmed, setConfirmed] = useState(false);
  const [authors, setAuthors] = useState<
    { canonicalId: string; document: { content: { title: string } } }[]
  >([]);
  const [authorPage, setAuthorPage] = useState(0);
  useEffect(() => {
    setAuthors([]);
    setAuthorPage(0);
  }, [selected?.language]);
  const pending = useRef(false);
  const load = async () =>
    setRows(await api<BlogView[]>("/admin/blog?page=" + page));
  useEffect(() => {
    let active = true;
    api<BlogView[]>("/admin/blog?page=" + page)
      .then((r) => {
        if (active) setRows(r);
      })
      .catch((e) => {
        if (active) setError(e);
      });
    return () => {
      active = false;
    };
  }, [page]);
  useEffect(() => {
    const handler = (e: BeforeUnloadEvent) => {
      if (dirty) {
        e.preventDefault();
        e.returnValue = "";
      }
    };
    window.addEventListener("beforeunload", handler);
    return () => window.removeEventListener("beforeunload", handler);
  }, [dirty]);
  const accept = (v: BlogView) => {
    setSelected(v);
    setDoc(v.document);
    setDirty(false);
    setPreview("");
    setHistory([]);
  };
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
  const change = (next: BlogDocument) => {
    setDoc(next);
    setDirty(true);
    setPreview("");
  };
  const allowed = edit && selected?.status !== 1 && selected?.status !== 2;
  const updateBlock = (i: number, next: Block) =>
    change({
      ...doc!,
      blocks: doc!.blocks.map((b, j) => (i === j ? next : b)),
    });
  const switchAllowed = () =>
    !dirty ||
    window.confirm(
      text(
        "Discard your unsaved changes?",
        "সংরক্ষণ না করা পরিবর্তনগুলো বাদ দেবেন?",
      ),
    );
  async function action(name: string, revisionId?: string) {
    if (!selected) return;
    accept(
      await api<BlogView>(
        `/admin/blog/${selected.id}/actions/${name}`,
        "POST",
        { expectedVersion: selected.version, revisionId },
      ),
    );
    setConfirm("");
    await load();
  }
  function blockEditor(b: Block, i: number) {
    const field = (
      key: "text" | "url" | "alt" | "citation",
      label: string,
      multiline = false,
    ) => (
      <TextField
        key={key}
        label={label}
        value={b[key] || ""}
        multiline={multiline}
        minRows={multiline ? 3 : undefined}
        onChange={(e) => updateBlock(i, { ...b, [key]: e.target.value })}
        fullWidth
      />
    );
    return (
      <Paper key={i} variant="outlined" sx={{ p: 2 }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
            <Typography sx={{ flex: 1 }}>
              {i + 1}. {b.type}
            </Typography>
            <Button
              disabled={i === 0}
              onClick={() => {
                const blocks = [...doc!.blocks];
                [blocks[i - 1], blocks[i]] = [blocks[i], blocks[i - 1]];
                change({ ...doc!, blocks });
              }}
            >
              {text("Move up", "উপরে")}
            </Button>
            <Button
              onClick={() =>
                change({
                  ...doc!,
                  blocks: doc!.blocks.filter((_, j) => i !== j),
                })
              }
            >
              {text("Remove", "বাদ দিন")}
            </Button>
          </Stack>
          {["Paragraph", "Heading", "Quote", "PullQuote"].includes(b.type) &&
            field("text", text("Text", "লেখা"), true)}
          {b.type === "Heading" && (
            <TextField
              select
              label={text("Heading level", "শিরোনামের স্তর")}
              value={b.level || 2}
              onChange={(e) =>
                updateBlock(i, { ...b, level: Number(e.target.value) })
              }
            >
              {[2, 3, 4].map((v) => (
                <MenuItem key={v} value={v}>
                  {v}
                </MenuItem>
              ))}
            </TextField>
          )}
          {["Image", "AudioEmbed"].includes(b.type) && (
            <>
              {field(
                "url",
                text("Media URL (HTTPS)", "মিডিয়ার ঠিকানা (HTTPS)"),
              )}
              {field("text", text("Caption", "বিবরণ"))}
            </>
          )}
          {b.type === "Image" &&
            field("alt", text("Image description", "ছবির বর্ণনা"))}
          {["Quote", "PullQuote"].includes(b.type) &&
            field("citation", text("Source / citation", "উৎস / উদ্ধৃতি"))}
          {b.type === "Gallery" && (
            <>
              {(b.images || []).map((image, j) => (
                <Stack key={j} spacing={1}>
                  <TextField
                    label={`Image ${j + 1} URL`}
                    value={image.url}
                    onChange={(e) =>
                      updateBlock(i, {
                        ...b,
                        images: b.images!.map((x, k) =>
                          j === k ? { ...x, url: e.target.value } : x,
                        ),
                      })
                    }
                  />
                  <TextField
                    label={`Image ${j + 1} description`}
                    value={image.alt}
                    onChange={(e) =>
                      updateBlock(i, {
                        ...b,
                        images: b.images!.map((x, k) =>
                          j === k ? { ...x, alt: e.target.value } : x,
                        ),
                      })
                    }
                  />
                  <Button
                    onClick={() =>
                      updateBlock(i, {
                        ...b,
                        images: b.images!.filter((_, k) => k !== j),
                      })
                    }
                  >
                    {text("Remove image", "ছবি বাদ দিন")}
                  </Button>
                </Stack>
              ))}
              <Button
                disabled={(b.images?.length || 0) >= 12}
                onClick={() =>
                  updateBlock(i, {
                    ...b,
                    images: [...(b.images || []), { url: "", alt: "" }],
                  })
                }
              >
                {text("Add image", "ছবি যোগ করুন")}
              </Button>
            </>
          )}
          {b.type === "RelatedPosts" && (
            <TextField
              select
              label={text("Related article", "সম্পর্কিত লেখা")}
              value=""
              onChange={(e) =>
                updateBlock(i, {
                  ...b,
                  postIds: [...new Set([...(b.postIds || []), e.target.value])],
                })
              }
            >
              {rows
                .filter(
                  (r) =>
                    r.postId !== selected?.postId &&
                    !(b.postIds || []).includes(r.postId),
                )
                .map((r) => (
                  <MenuItem key={r.id} value={r.postId}>
                    {r.document.title} ({r.language})
                  </MenuItem>
                ))}
            </TextField>
          )}
          {b.type === "RelatedPosts" &&
            (b.postIds || []).map((id) => (
              <Button
                key={id}
                onClick={() =>
                  updateBlock(i, {
                    ...b,
                    postIds: b.postIds!.filter((x) => x !== id),
                  })
                }
              >
                {text("Remove", "বাদ দিন")}:{" "}
                {rows.find((r) => r.postId === id)?.document.title || id}
              </Button>
            ))}
        </Stack>
      </Paper>
    );
  }
  return (
    <>
      <Heading
        title={text("Blog publishing", "ব্লগ প্রকাশনা")}
        subtitle={text(
          "Every translation requires administrator approval before publication.",
          "প্রতিটি ভাষার লেখা প্রকাশের আগে প্রশাসকের অনুমোদন প্রয়োজন।",
        )}
      />
      <ErrorBox error={error} />
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: { xs: "1fr", lg: "260px 1fr" },
          gap: 3,
        }}
      >
        <Paper sx={{ p: 2, alignSelf: "start" }}>
          <Stack spacing={2}>
            {edit && (
              <Stack direction="row" spacing={1}>
                {["bn", "en"].map((language) => (
                  <Button
                    key={language}
                    disabled={busy}
                    onClick={() => {
                      if (switchAllowed())
                        void run(async () => {
                          accept(
                            await api<BlogView>("/admin/blog", "POST", {
                              language,
                            }),
                          );
                          await load();
                        });
                    }}
                  >
                    {language === "bn" ? "নতুন লেখা" : "New article"}
                  </Button>
                ))}
              </Stack>
            )}
            {rows.map((r) => (
              <Button
                key={r.id}
                disabled={busy}
                variant={selected?.id === r.id ? "contained" : "text"}
                sx={{ justifyContent: "start", textAlign: "left" }}
                onClick={() => {
                  if (switchAllowed())
                    void run(async () =>
                      accept(await api<BlogView>("/admin/blog/" + r.id)),
                    );
                }}
              >
                {r.document.title} · {r.language} · {states[r.status]}
              </Button>
            ))}
            <Stack direction="row">
              <Button
                disabled={page === 1 || busy}
                onClick={() => {
                  if (switchAllowed()) {
                    setSelected(undefined);
                    setDoc(undefined);
                    setDirty(false);
                    setPage((p) => p - 1);
                  }
                }}
              >
                {text("Previous", "আগের")}
              </Button>
              <Button
                disabled={rows.length < 20 || busy}
                onClick={() => {
                  if (switchAllowed()) {
                    setSelected(undefined);
                    setDoc(undefined);
                    setDirty(false);
                    setPage((p) => p + 1);
                  }
                }}
              >
                {text("Next", "পরের")}
              </Button>
            </Stack>
          </Stack>
        </Paper>
        {selected && doc ? (
          <Stack spacing={3}>
            <Alert severity="info">
              {states[selected.status]} · {selected.language} ·{" "}
              {text("Version", "সংস্করণ")} {selected.version}
              {selected.publishedRevisionId
                ? " · " +
                  text(
                    "An approved version is live.",
                    "অনুমোদিত সংস্করণ প্রকাশিত আছে।",
                  )
                : ""}
              {dirty
                ? " · " + text("Unsaved changes", "পরিবর্তন সংরক্ষিত হয়নি")
                : ""}
            </Alert>
            <Box
              component="fieldset"
              disabled={busy || !allowed}
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
                        text("Title", "শিরোনাম"),
                        text("Page address", "পাতার ঠিকানা"),
                        text("Introduction", "ভূমিকা"),
                        text("Search title", "সার্চ শিরোনাম"),
                        text("Search description", "সার্চ বিবরণ"),
                      ][i]
                    }
                    value={doc[key]}
                    multiline={key === "summary" || key === "seoDescription"}
                    onChange={(e) => change({ ...doc, [key]: e.target.value })}
                  />
                ))}
                <TextField
                  label={text(
                    "Tags (comma separated)",
                    "ট্যাগ (কমা দিয়ে আলাদা করুন)",
                  )}
                  value={doc.tags.join(", ")}
                  onChange={(e) =>
                    change({
                      ...doc,
                      tags: e.target.value.split(",").map((t) => t.trim()),
                    })
                  }
                />
                <FormControlLabel
                  label={text(
                    "AI assisted this text, translation or tags",
                    "লেখা, অনুবাদ বা ট্যাগে AI সহায়তা নেওয়া হয়েছে",
                  )}
                  control={
                    <Checkbox
                      checked={doc.provenance !== "human"}
                      disabled={selected.document.provenance !== "human"}
                      onChange={(e) =>
                        change({
                          ...doc,
                          provenance: e.target.checked
                            ? "ai_assisted_draft"
                            : "human",
                        })
                      }
                    />
                  }
                />
                <Typography variant="h6">
                  {text("Co-authors", "সহলেখক")}
                </Typography>
                <Button
                  disabled={busy}
                  onClick={() =>
                    void run(async () => {
                      const next = authorPage + 1;
                      const found = await api<typeof authors>(
                        `/website/AuthorProfile/${selected.language}?page=${next}`,
                      );
                      setAuthors((old) =>
                        [...old, ...found].filter(
                          (v, i, a) =>
                            a.findIndex(
                              (x) => x.canonicalId === v.canonicalId,
                            ) === i,
                        ),
                      );
                      setAuthorPage(next);
                    })
                  }
                >
                  {text(
                    "Load published author profiles",
                    "প্রকাশিত লেখক প্রোফাইল দেখুন",
                  )}
                </Button>
                {authors.map((a) => (
                  <FormControlLabel
                    key={a.canonicalId}
                    label={a.document.content.title}
                    control={
                      <Checkbox
                        disabled={!allowed || busy}
                        checked={(doc.authorIds || []).includes(a.canonicalId)}
                        onChange={(e) =>
                          change({
                            ...doc,
                            authorIds: e.target.checked
                              ? [...(doc.authorIds || []), a.canonicalId]
                              : (doc.authorIds || []).filter(
                                  (id) => id !== a.canonicalId,
                                ),
                          })
                        }
                      />
                    }
                  />
                ))}
                {(doc.authorIds || [])
                  .filter((id) => !authors.some((a) => a.canonicalId === id))
                  .map((id) => (
                    <FormControlLabel
                      key={id}
                      label={text(
                        "Saved author (load profiles to see name)",
                        "সংরক্ষিত লেখক (নাম দেখতে প্রোফাইল দেখুন)",
                      )}
                      control={
                        <Checkbox
                          disabled={!allowed || busy}
                          checked
                          onChange={() =>
                            change({
                              ...doc,
                              authorIds: (doc.authorIds || []).filter(
                                (x) => x !== id,
                              ),
                            })
                          }
                        />
                      }
                    />
                  ))}
                {doc.blocks.map(blockEditor)}
                <TextField
                  select
                  label={text("Add content", "বিষয়বস্তু যোগ করুন")}
                  value=""
                  onChange={(e) =>
                    change({
                      ...doc,
                      blocks: [
                        ...doc.blocks,
                        {
                          type: e.target.value as Block["type"],
                          ...(e.target.value === "Heading" ? { level: 2 } : {}),
                        },
                      ],
                    })
                  }
                >
                  {blockTypes.map((type) => (
                    <MenuItem value={type} key={type}>
                      {type}
                    </MenuItem>
                  ))}
                </TextField>
              </Stack>
            </Box>
            <Stack
              direction="row"
              spacing={1}
              useFlexGap
              sx={{ flexWrap: "wrap" }}
            >
              <Button
                disabled={busy || !allowed || !dirty}
                variant="contained"
                onClick={() =>
                  void run(async () => {
                    accept(
                      await api<BlogView>("/admin/blog/" + selected.id, "PUT", {
                        expectedVersion: selected.version,
                        document: { ...doc, tags: doc.tags.filter(Boolean) },
                      }),
                    );
                    await load();
                  })
                }
              >
                {text("Save draft", "খসড়া সংরক্ষণ")}
              </Button>
              <Button
                disabled={busy || dirty}
                onClick={() =>
                  void run(async () => {
                    const result = await api<{ token: string }>(
                      `/admin/blog/${selected.id}/preview`,
                      "POST",
                      {},
                    );
                    const origin =
                      import.meta.env.VITE_WEBSITE_ORIGIN ||
                      "http://localhost:3000";
                    setPreview(
                      `${origin}/${selected.language}/blog/preview#${encodeURIComponent(result.token)}`,
                    );
                  })
                }
              >
                {text("Private preview", "ব্যক্তিগত প্রিভিউ")}
              </Button>
              {edit && selected.status === 0 && (
                <Button
                  disabled={
                    busy || dirty || doc.provenance === "ai_assisted_draft"
                  }
                  onClick={() => void run(() => action("review"))}
                >
                  {text("Send for review", "পর্যালোচনায় পাঠান")}
                </Button>
              )}
              {edit && [1, 2].includes(selected.status) && (
                <Button
                  disabled={busy}
                  onClick={() => void run(() => action("return"))}
                >
                  {text("Return to draft", "খসড়ায় ফেরান")}
                </Button>
              )}
              {admin &&
                selected.status === 0 &&
                doc.provenance === "ai_assisted_draft" && (
                  <Button
                    disabled={busy || dirty}
                    onClick={() => {
                      setConfirm("review-ai");
                      setConfirmed(false);
                    }}
                  >
                    {text(
                      "Confirm human review",
                      "মানুষের পর্যালোচনা নিশ্চিত করুন",
                    )}
                  </Button>
                )}
              {admin && selected.status === 1 && (
                <Button
                  disabled={busy}
                  onClick={() => {
                    setConfirm("approve");
                    setConfirmed(false);
                  }}
                >
                  {text("Approve", "অনুমোদন")}
                </Button>
              )}
              {admin && selected.status === 2 && (
                <Button
                  disabled={busy}
                  onClick={() => {
                    setConfirm("publish");
                    setConfirmed(false);
                  }}
                >
                  {text("Publish", "প্রকাশ করুন")}
                </Button>
              )}
              {admin && selected.status !== 4 && (
                <Button
                  color="warning"
                  disabled={busy || dirty}
                  onClick={() => {
                    setConfirm("archive");
                    setConfirmed(false);
                  }}
                >
                  {text("Archive", "আর্কাইভ করুন")}
                </Button>
              )}
              <Button
                disabled={busy}
                onClick={() =>
                  void run(async () =>
                    setHistory(
                      await api<Revision[]>(
                        `/admin/blog/${selected.id}/revisions`,
                      ),
                    ),
                  )
                }
              >
                {text("Revision history", "সংস্করণের ইতিহাস")}
              </Button>
              {edit && (
                <Button
                  disabled={busy || dirty}
                  onClick={() =>
                    void run(async () => {
                      accept(
                        await api<BlogView>("/admin/blog", "POST", {
                          language: selected.language === "bn" ? "en" : "bn",
                          postId: selected.postId,
                        }),
                      );
                      await load();
                    })
                  }
                >
                  {text("Add other language", "অন্য ভাষা যোগ করুন")}
                </Button>
              )}
            </Stack>
            {preview && (
              <Alert severity="warning">
                <Typography>
                  {text(
                    "Private link: expires in 15 minutes or when the saved version changes.",
                    "ব্যক্তিগত লিংক: ১৫ মিনিট বা সংস্করণ বদলানো পর্যন্ত কার্যকর।",
                  )}
                </Typography>
                <a href={preview} target="_blank" rel="noreferrer">
                  {text("Open preview", "প্রিভিউ খুলুন")}
                </a>
                <TextField
                  fullWidth
                  value={preview}
                  slotProps={{ input: { readOnly: true } }}
                  label={text(
                    "Copy private preview link",
                    "ব্যক্তিগত প্রিভিউ লিংক কপি করুন",
                  )}
                />
              </Alert>
            )}
            {selected.approvedBy && (
              <Typography>
                {text("Approved by", "অনুমোদনকারী")}: {selected.approvedBy} ·{" "}
                {selected.approvedAtUtc}
              </Typography>
            )}
            {history.length > 0 && (
              <Paper sx={{ p: 2 }}>
                <Typography variant="h6">
                  {text(
                    "History — restoring creates a new draft",
                    "ইতিহাস — ফিরিয়ে আনলে নতুন খসড়া হবে",
                  )}
                </Typography>
                {history.map((r) => (
                  <Stack
                    key={r.id}
                    direction="row"
                    spacing={2}
                    sx={{ alignItems: "center" }}
                  >
                    <Typography sx={{ flex: 1 }}>
                      {r.version} · {r.action} · {r.createdAtUtc} · {r.actorId}
                    </Typography>
                    {edit && (
                      <Button
                        disabled={busy || dirty}
                        onClick={() => {
                          setConfirm("restore:" + r.id);
                          setConfirmed(false);
                        }}
                      >
                        {text("Restore", "ফিরিয়ে আনুন")}
                      </Button>
                    )}
                  </Stack>
                ))}
              </Paper>
            )}
          </Stack>
        ) : (
          <Alert severity="info">
            {text(
              "Select an article or create a new draft.",
              "একটি লেখা বেছে নিন বা নতুন খসড়া তৈরি করুন।",
            )}
          </Alert>
        )}
      </Box>
      <Dialog open={!!confirm} onClose={() => !busy && setConfirm("")}>
        <DialogTitle>
          {text("Confirm publishing action", "প্রকাশনার কাজ নিশ্চিত করুন")}
        </DialogTitle>
        <DialogContent>
          <Typography>
            {confirm.startsWith("restore")
              ? text(
                  "Restore this revision as a draft. The current public version stays unchanged until approved and published again.",
                  "এই সংস্করণটি খসড়া হিসেবে ফিরবে। আবার অনুমোদন ও প্রকাশের আগে প্রকাশিত লেখা বদলাবে না।",
                )
              : confirm === "archive"
                ? text(
                    "Remove this translation from public reading.",
                    "এই ভাষার লেখাটি জনসাধারণের পড়া থেকে সরিয়ে দিন।",
                  )
                : confirm === "review-ai"
                  ? text(
                      "I reviewed the AI-assisted text, translation, tags and references for accuracy.",
                      "আমি AI-সহায়তায় তৈরি লেখা, অনুবাদ, ট্যাগ ও তথ্যসূত্র যাচাই করেছি।",
                    )
                  : text(
                      `Proceed with ${confirm} for this saved version?`,
                      `সংরক্ষিত সংস্করণে ${confirm} কার্যকর করবেন?`,
                    )}
          </Typography>
          <FormControlLabel
            label={text("I confirm this action.", "আমি এই কাজ নিশ্চিত করছি।")}
            control={
              <Checkbox
                checked={confirmed}
                onChange={(e) => setConfirmed(e.target.checked)}
              />
            }
          />
        </DialogContent>
        <DialogActions>
          <Button disabled={busy} onClick={() => setConfirm("")}>
            {text("Cancel", "বাতিল")}
          </Button>
          <Button
            disabled={busy || !confirmed}
            onClick={() =>
              void run(() =>
                action(confirm.split(":")[0], confirm.split(":")[1]),
              )
            }
          >
            {text("Confirm", "নিশ্চিত করুন")}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
