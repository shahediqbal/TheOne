import type { LineageTreeNode } from "../../lib/website-api";
import { assetUrl } from "../../lib/website-api";

/**
 * A plain nested-list lineage tree: portrait, name, life dates, each generation indented under
 * its parent. Plain <ul>/<li> nesting, deliberately no tree/treeitem ARIA roles — this is a
 * static, non-interactive list with no expand/collapse or keyboard navigation, and those roles
 * would tell assistive tech to expect tree-widget behavior that doesn't exist, which is worse
 * than using no role at all. Not the fuller interactive/graphical genealogy view on the roadmap —
 * that's a distinct, bigger visual component worth scoping separately once this data is live.
 */
export function LineageTree({ nodes }: { nodes: LineageTreeNode[] }) {
  if (!nodes.length) return null;
  return (
    <ul className="lineage-tree">
      {nodes.map((node) => (
        <li key={node.canonicalId}>
          <div className="lineage-node">
            {assetUrl(node.document.fields.portraitAssetId) ? (
              <img
                src={assetUrl(node.document.fields.portraitAssetId)}
                alt=""
                loading="lazy"
                referrerPolicy="no-referrer"
                className="lineage-portrait"
              />
            ) : (
              <span className="lineage-portrait lineage-portrait-empty" aria-hidden="true" />
            )}
            <div>
              <p className="lineage-name">{node.document.content.title}</p>
              {node.document.fields.lifeDates ? (
                <p className="lineage-dates">{node.document.fields.lifeDates}</p>
              ) : null}
            </div>
          </div>
          {node.children.length ? <LineageTree nodes={node.children} /> : null}
        </li>
      ))}
    </ul>
  );
}
