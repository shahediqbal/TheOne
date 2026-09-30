"use client";
import { useRef, useState, type ReactNode } from "react";
export function ResponsiveNav({ language, children }: {language: "bn" | "en"; children: ReactNode}) {
  const [open, setOpen] = useState(false);
  const toggle = useRef<HTMLButtonElement>(null);
  return <div className="navigation-shell" onKeyDown={event => {
    if (event.key === "Escape") { setOpen(false); toggle.current?.focus(); }
  }}>
    <button ref={toggle} className="mobile-menu-button" aria-expanded={open} aria-controls="public-navigation" onClick={() => setOpen(!open)}>
      <span aria-hidden="true">{open ? "×" : "☰"}</span> {language === "en" ? "Menu" : "মেনু"}
    </button>
    <nav id="public-navigation" className="site-nav" aria-label="Main" data-open={open} onClick={event => {
      if ((event.target as HTMLElement).closest("a")) setOpen(false);
    }}>{children}</nav>
  </div>;
}
