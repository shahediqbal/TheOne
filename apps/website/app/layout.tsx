import "./globals.css";
export const metadata = {
  title: "Sadria Society | Membership",
  description:
    "Membership registration and development form for Sadria Society.",
};
export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="bn">
      <body>{children}</body>
    </html>
  );
}
