import type { MenuTreeNode } from "../../lib/website-api";

/**
 * Renders one menu entry at any depth. A leaf is a plain link. A parent renders its own link
 * plus a separate native <details>/<summary> disclosure toggle for its children — deliberately
 * NOT a link nested inside <summary>, since a click's navigate-vs-toggle behavior when one
 * interactive element is nested inside another is browser-dependent and not something to rely
 * on without a real browser to verify against. Two independent controls avoids the ambiguity
 * entirely, and <details> gives correct keyboard/touch disclosure behavior for free.
 */
export function NavItem({ item, depth }: { item: MenuTreeNode; depth: number }) {
  if (item.children.length === 0) {
    return (
      <a href={item.href} className="site-nav-link">
        {item.label}
      </a>
    );
  }
  return (
    <div className={`site-nav-item-with-children site-nav-depth-${depth}`}>
      <a href={item.href} className="site-nav-link">
        {item.label}
      </a>
      <details className="site-nav-details">
        <summary className="site-nav-toggle" aria-label={`Toggle ${item.label} submenu`} />
        <div className="site-nav-dropdown">
          {item.children.map((child) => (
            <NavItem key={child.key} item={child} depth={depth + 1} />
          ))}
        </div>
      </details>
    </div>
  );
}
