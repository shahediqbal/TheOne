import {
  Box,
  Button,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { blockTypes, type Block } from "../../../../packages/blog/types";
export default function BlocksEditor({
  blocks,
  onChange,
}: {
  blocks: Block[];
  onChange: (blocks: Block[]) => void;
}) {
  const update = (i: number, b: Block) =>
    onChange(blocks.map((v, j) => (i === j ? b : v)));
  return (
    <Stack spacing={2}>
      {blocks.map((b, i) => (
        <Paper variant="outlined" key={i} sx={{ p: 2 }}>
          <Stack spacing={2}>
            <Box sx={{ display: "flex", gap: 1, alignItems: "center" }}>
              <Typography sx={{ flex: 1 }}>
                {i + 1}. {b.type}
              </Typography>
              <Button
                disabled={i === 0}
                onClick={() => {
                  const copy = [...blocks];
                  [copy[i - 1], copy[i]] = [copy[i], copy[i - 1]];
                  onChange(copy);
                }}
              >
                ↑
              </Button>
              <Button
                onClick={() => onChange(blocks.filter((_, j) => i !== j))}
              >
                Remove
              </Button>
            </Box>
            {[
              "Paragraph",
              "Heading",
              "Quote",
              "PullQuote",
              "Image",
              "AudioEmbed",
            ].includes(b.type) && (
              <TextField
                multiline
                minRows={2}
                label={
                  ["Image", "AudioEmbed"].includes(b.type) ? "Caption" : "Text"
                }
                value={b.text || ""}
                onChange={(e) => update(i, { ...b, text: e.target.value })}
              />
            )}
            {b.type === "Heading" && (
              <TextField
                select
                label="Heading level"
                value={b.level || 2}
                onChange={(e) =>
                  update(i, { ...b, level: Number(e.target.value) })
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
              <TextField
                label="HTTPS media URL"
                value={b.url || ""}
                onChange={(e) => update(i, { ...b, url: e.target.value })}
              />
            )}
            {b.type === "Image" && (
              <TextField
                label="Image description"
                value={b.alt || ""}
                onChange={(e) => update(i, { ...b, alt: e.target.value })}
              />
            )}
            {["Quote", "PullQuote"].includes(b.type) && (
              <TextField
                label="Source / citation"
                value={b.citation || ""}
                onChange={(e) => update(i, { ...b, citation: e.target.value })}
              />
            )}
            {b.type === "Gallery" && (
              <>
                {(b.images || []).map((image, j) => (
                  <Stack key={j} spacing={1}>
                    {(["url", "alt", "caption"] as const).map((key) => (
                      <TextField
                        key={key}
                        label={`Image ${j + 1}: ${key}`}
                        value={image[key] || ""}
                        onChange={(e) =>
                          update(i, {
                            ...b,
                            images: b.images!.map((v, k) =>
                              k === j ? { ...v, [key]: e.target.value } : v,
                            ),
                          })
                        }
                      />
                    ))}
                    <Button
                      onClick={() =>
                        update(i, {
                          ...b,
                          images: b.images!.filter((_, k) => k !== j),
                        })
                      }
                    >
                      Remove image
                    </Button>
                  </Stack>
                ))}
                <Button
                  disabled={(b.images?.length || 0) >= 12}
                  onClick={() =>
                    update(i, {
                      ...b,
                      images: [...(b.images || []), { url: "", alt: "" }],
                    })
                  }
                >
                  Add image
                </Button>
              </>
            )}
            {b.type === "RelatedPosts" && (
              <TextField
                label="Related article IDs (one per line)"
                helperText="Use canonical IDs from the Blog publishing screen."
                multiline
                value={(b.postIds || []).join("\n")}
                onChange={(e) =>
                  update(i, {
                    ...b,
                    postIds: e.target.value
                      .split("\n")
                      .map((v) => v.trim())
                      .filter(Boolean),
                  })
                }
              />
            )}
          </Stack>
        </Paper>
      ))}
      <TextField
        select
        label="Add content block"
        value=""
        onChange={(e) =>
          onChange([
            ...blocks,
            {
              type: e.target.value as Block["type"],
              ...(e.target.value === "Heading" ? { level: 2 } : {}),
            },
          ])
        }
      >
        {blockTypes.map((type) => (
          <MenuItem key={type} value={type}>
            {type}
          </MenuItem>
        ))}
      </TextField>
    </Stack>
  );
}
