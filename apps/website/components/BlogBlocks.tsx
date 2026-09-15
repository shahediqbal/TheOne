import { createElement } from "react";
import { safeUrl, type Block } from "../../../packages/blog/types";
export default function BlogBlocks({
  blocks,
  language,
}: {
  blocks: Block[];
  language: string;
}) {
  return (
    <div className="blog-prose">
      {blocks.map((b, i) => {
        switch (b.type) {
          case "Paragraph":
            return <p key={i}>{b.text}</p>;
          case "Heading":
            return createElement(
              "h" + ([2, 3, 4].includes(b.level || 0) ? b.level : 2),
              { key: i },
              b.text,
            );
          case "Image":
            return safeUrl(b.url) ? (
              <figure key={i}>
                <img
                  src={b.url}
                  alt={b.alt || ""}
                  loading="lazy"
                  referrerPolicy="no-referrer"
                />
                {b.text && <figcaption>{b.text}</figcaption>}
              </figure>
            ) : null;
          case "Gallery":
            return (
              <div className="blog-gallery" key={i}>
                {b.images
                  ?.filter((image) => safeUrl(image.url))
                  .map((image, j) => (
                    <figure key={j}>
                      <img
                        src={image.url}
                        alt={image.alt}
                        loading="lazy"
                        referrerPolicy="no-referrer"
                      />
                      {image.caption && (
                        <figcaption>{image.caption}</figcaption>
                      )}
                    </figure>
                  ))}
              </div>
            );
          case "Quote":
          case "PullQuote":
            return (
              <blockquote key={i}>
                <p>{b.text}</p>
                {b.citation && <cite>{b.citation}</cite>}
              </blockquote>
            );
          case "AudioEmbed":
            return safeUrl(b.url) ? (
              <figure key={i}>
                <audio controls preload="none" src={b.url} />
                {b.text && <figcaption>{b.text}</figcaption>}
              </figure>
            ) : null;
          case "RelatedPosts":
            return (
              <aside key={i}>
                <strong>
                  {language === "bn" ? "সম্পর্কিত লেখা" : "Related reading"}
                </strong>
                <ul>
                  {b.postIds?.map((id, j) => (
                    <li key={id}>
                      <a
                        href={`/${language}/blog/post/${encodeURIComponent(id)}`}
                      >
                        {language === "bn" ? "লেখা" : "Article"} {j + 1}
                      </a>
                    </li>
                  ))}
                </ul>
              </aside>
            );
          default:
            return null;
        }
      })}
    </div>
  );
}
