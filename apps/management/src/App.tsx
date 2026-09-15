import { useState, lazy, Suspense } from "react";
import { Routes, Route, NavLink, Link, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import {
  Alert,
  Avatar,
  Box,
  Button,
  Divider,
  Drawer,
  IconButton,
  Paper,
  Stack,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import LogoutIcon from "@mui/icons-material/Logout";
import DashboardIcon from "@mui/icons-material/DashboardOutlined";
import ShieldIcon from "@mui/icons-material/ShieldOutlined";
import { useAuth } from "./auth";
import Login from "./Login";
import { Heading, ErrorBox, Loading } from "./ui";
const Website = lazy(() => import("./pages/Website"));
const Blog = lazy(() => import("./pages/Blog"));
const Membership = lazy(() => import("./pages/Membership"));
const Users = lazy(() => import("./pages/Users"));
const Roles = lazy(() => import("./pages/Roles"));
const Menus = lazy(() => import("./pages/Menus"));
const Audit = lazy(() => import("./pages/Audit"));
const Security = lazy(() => import("./pages/Security"));
import type { Nav } from "./types";
const known = new Set([
  "/website",
  "/blog",
  "/membership",
  "/administration/users",
  "/administration/roles",
  "/administration/menus",
  "/administration/audit",
]);
export default function App() {
  const auth = useAuth();
  const { t, i18n } = useTranslation();
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<unknown>();
  const wide = useMediaQuery(useTheme().breakpoints.up("md"));
  const location = useLocation();
  if (auth.busy)
    return (
      <Box
        sx={{
          p: 5,
        }}
      >
        <Loading />
      </Box>
    );
  if (!auth.profile) return <Login />;
  const link = (to: string, label: string, depth = 0) => (
    <Button
      fullWidth
      key={to}
      component={NavLink}
      to={to}
      onClick={() => setOpen(false)}
      startIcon={depth === 0 ? <DashboardIcon /> : undefined}
      sx={{
        justifyContent: "flex-start",
        color: "#d6e4e7",
        borderRadius: 1,
        pl: 2 + depth * 2,
        "&.active": {
          bgcolor: "#ffffff15",
          color: "#fff",
          boxShadow: "inset 3px 0 #dfb65b",
        },
        "&:hover": {
          bgcolor: "#ffffff12",
        },
      }}
    >
      {label}
    </Button>
  );
  const nav = (items: Nav[], depth = 0): React.ReactNode =>
    items.map((n) => (
      <Box key={n.id}>
        {n.route && known.has(n.route) ? (
          link(
            n.route,
            i18n.language === "bn" && n.labelBn ? n.labelBn : n.labelEn,
            depth,
          )
        ) : n.children.length > 0 ? (
          <Typography
            sx={{
              px: 2,
              mt: 3,
              mb: 1,
              color: "#9fbfc5",
              fontSize: 12,
              letterSpacing: 1,
            }}
          >
            {i18n.language === "bn" && n.labelBn ? n.labelBn : n.labelEn}
          </Typography>
        ) : null}
        {n.children.length > 0 && nav(n.children, depth + 1)}
      </Box>
    ));
  const side = (
    <Stack
      sx={{
        height: "100%",
        p: 2.5,
      }}
    >
      <Stack
        direction="row"
        sx={{
          gap: 1.5,
          alignItems: "center",
          px: 1,
          py: 2,
          mb: 3,
        }}
      >
        <span className="brand-mark">১</span>
        <Box>
          <Typography
            variant="h6"
            sx={{
              color: "white",
            }}
          >
            The One
          </Typography>
          <Typography
            variant="caption"
            sx={{
              color: "#a7c5cb",
            }}
          >
            MANAGEMENT
          </Typography>
        </Box>
      </Stack>
      {link("/", t("Overview"))}
      {nav(auth.menus)}
      <Divider
        sx={{
          my: 3,
          borderColor: "#ffffff18",
        }}
      />
      {link("/account", t("My account"))}
      {link("/account/security", t("Security"))}
      <Box
        sx={{
          flex: 1,
        }}
      />
      <Typography
        variant="body2"
        sx={{
          color: "#9fbfc5",
          ...{
            px: 1,
            py: 2,
          },
        }}
      >
        {auth.profile.roles.join(" · ")}
      </Typography>
    </Stack>
  );
  const guard = (permission: string, node: React.ReactNode) =>
    auth.permissions.includes(permission) ? (
      node
    ) : (
      <Alert severity="warning">{t("Access denied")}</Alert>
    );
  const owner = (node: React.ReactNode) =>
    auth.profile!.roles.includes("SuperAdmin") ? (
      node
    ) : (
      <Alert severity="warning">{t("Access denied")}</Alert>
    );
  return (
    <Box
      sx={{
        display: "flex",
        minHeight: "100vh",
      }}
    >
      <Drawer
        variant={wide ? "permanent" : "temporary"}
        open={wide || open}
        onClose={() => setOpen(false)}
        sx={{
          width: wide ? 256 : 0,
          flexShrink: 0,
          "& .MuiDrawer-paper": {
            width: 256,
            bgcolor: "#123e48",
            border: 0,
          },
        }}
      >
        {side}
      </Drawer>
      <Box
        sx={{
          flex: 1,
          minWidth: 0,
        }}
      >
        <Box
          component="header"
          sx={{
            height: 80,
            bgcolor: "white",
            borderBottom: "1px solid #e1e8ec",
            px: {
              xs: 2,
              md: 4,
            },
            display: "flex",
            alignItems: "center",
            gap: 2,
          }}
        >
          {!wide && (
            <IconButton aria-label="Open menu" onClick={() => setOpen(true)}>
              <MenuIcon />
            </IconButton>
          )}
          <Typography
            sx={{
              color: "text.secondary",
              ...{
                flex: 1,
              },
            }}
          >
            {t("Your workspace")}
          </Typography>
          <Button
            onClick={() =>
              i18n.changeLanguage(i18n.language === "en" ? "bn" : "en")
            }
          >
            {i18n.language === "en" ? "বাংলা" : "English"}
          </Button>
          <Avatar
            sx={{
              width: 36,
              height: 36,
              bgcolor: "#e5eff1",
              color: "primary.main",
            }}
          >
            {auth.profile.fullName?.[0] || "U"}
          </Avatar>
          <IconButton
            aria-label={t("Sign out")}
            onClick={() => auth.signOut().catch(setError)}
          >
            <LogoutIcon />
          </IconButton>
        </Box>
        <Box
          component="main"
          sx={{
            p: {
              xs: 2,
              sm: 3,
              lg: 4,
            },
            maxWidth: 1600,
            mx: "auto",
          }}
        >
          <ErrorBox error={error} />
          <Suspense fallback={<Loading />}>
            <Routes key={location.pathname}>
              <Route
                path="/"
                element={
                  <>
                    <Heading
                      title="Overview"
                      subtitle={auth.profile.fullName}
                    />
                    <Paper
                      variant="outlined"
                      sx={{
                        p: 4,
                        borderTop: "3px solid #d4a447",
                      }}
                    >
                      <Stack
                        direction="row"
                        sx={{
                          alignItems: "center",
                          gap: 2,
                          mb: 2,
                        }}
                      >
                        <ShieldIcon color="primary" />
                        <Typography variant="h5">
                          {t("Your workspace")}
                        </Typography>
                      </Stack>
                      <Typography
                        sx={{
                          color: "text.secondary",
                          ...{
                            mb: 3,
                          },
                        }}
                      >
                        Choose a section from your menu, or manage your account
                        and security settings.
                      </Typography>
                      <Stack
                        direction="row"
                        sx={{
                          gap: 2,
                          flexWrap: "wrap",
                        }}
                      >
                        <Button
                          component={Link}
                          to="/account"
                          variant="contained"
                        >
                          {t("My account")}
                        </Button>
                        <Button
                          component={Link}
                          to="/account/security"
                          variant="outlined"
                        >
                          {t("Security")}
                        </Button>
                        {auth.permissions.includes("users.read") && (
                          <Button component={Link} to="/administration/users">
                            {t("Users")}
                          </Button>
                        )}
                      </Stack>
                    </Paper>
                  </>
                }
              />
              <Route
                path="/account"
                element={
                  <>
                    <Heading title="My account" />
                    <Paper
                      variant="outlined"
                      sx={{
                        p: 3,
                      }}
                    >
                      <Stack
                        sx={{
                          gap: 2,
                        }}
                      >
                        {[
                          ["Full name", auth.profile.fullName],
                          ["Email", auth.profile.email],
                          ["Mobile number", auth.profile.mobileNumber],
                          ["Roles", auth.profile.roles.join(", ")],
                        ].map(([label, value]) => (
                          <Box key={label}>
                            <Typography
                              variant="body2"
                              sx={{
                                color: "text.secondary",
                              }}
                            >
                              {t(label)}
                            </Typography>
                            <Typography>{value || "—"}</Typography>
                          </Box>
                        ))}
                        <Button component={Link} to="/account/security">
                          {t("Security")}
                        </Button>
                      </Stack>
                    </Paper>
                  </>
                }
              />
              <Route path="/website" element={guard("website.read", <Website />)} />
              <Route path="/blog" element={guard("blog.read", <Blog />)} />
              <Route
                path="/membership"
                element={guard("membership.read", <Membership />)}
              />
              <Route
                path="/membership/:reference"
                element={guard("membership.read", <Membership />)}
              />
              <Route path="/account/security" element={<Security />} />
              <Route
                path="/administration/users"
                element={guard("users.read", <Users />)}
              />
              <Route path="/administration/roles" element={owner(<Roles />)} />
              <Route path="/administration/menus" element={owner(<Menus />)} />
              <Route
                path="/administration/audit"
                element={guard("audit.read", <Audit />)}
              />
              <Route path="*" element={<Heading title="Page not found" />} />
            </Routes>
          </Suspense>
        </Box>
      </Box>
    </Box>
  );
}
