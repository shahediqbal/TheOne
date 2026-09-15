import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createTheme, ThemeProvider, CssBaseline } from "@mui/material";
import { AuthProvider } from "./auth";
import App from "./App";
import "./i18n";
import "./style.css";
const theme = createTheme({
  palette: {
    primary: {
      main: "#145664",
      dark: "#103e48",
    },
    secondary: {
      main: "#d4a447",
    },
    background: {
      default: "#f4f7f9",
      paper: "#fff",
    },
    text: {
      primary: "#18313b",
      secondary: "#5f7079",
    },
  },
  typography: {
    fontFamily: '"Segoe UI", "Nirmala UI", sans-serif',
    h4: {
      fontSize: "1.8rem",
      fontWeight: 700,
    },
    h5: {
      fontWeight: 700,
    },
    button: {
      textTransform: "none",
      fontWeight: 600,
    },
  },
  shape: {
    borderRadius: 10,
  },
  components: {
    MuiButton: {
      defaultProps: {
        disableElevation: true,
      },
      styleOverrides: {
        root: {
          minHeight: 42,
        },
      },
    },
    MuiTextField: {
      defaultProps: {
        fullWidth: true,
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: "none",
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        head: {
          fontWeight: 700,
          background: "#f5f8fa",
        },
        root: {
          fontSize: 14,
          padding: "16px",
        },
      },
    },
  },
});
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      staleTime: 30000,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: false,
    },
  },
});
ReactDOM.createRoot(document.getElementById("root")!).render(
  <ThemeProvider theme={theme}>
    <CssBaseline />
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </ThemeProvider>,
);
