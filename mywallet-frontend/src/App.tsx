// src/App.tsx
import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";

import Login from "./pages/Login";
import Register from "./pages/Register";
import Dashboard from "./pages/Dashboard";
import PortfolioDetails from "./pages/PortfolioDetails";
import PortfolioReportForm from "./pages/PortfolioReportForm";
import PortfolioReportView from "./pages/PortfolioReportView";

// Import nowej strony wykresu
import PortfolioChart from "./pages/PortfolioChart";

import "./App.css";
import "./auth.css";

function App() {
    const isAuthenticated = () => localStorage.getItem("user") !== null;

    return (
        <BrowserRouter>
            <Routes>
                <Route
                    path="/"
                    element={isAuthenticated() ? <Navigate to="/dashboard" /> : <Login />}
                />
                <Route
                    path="/register"
                    element={isAuthenticated() ? <Navigate to="/dashboard" /> : <Register />}
                />
                <Route
                    path="/dashboard"
                    element={isAuthenticated() ? <Dashboard /> : <Navigate to="/" />}
                />
                <Route
                    path="/portfolio/:id"
                    element={isAuthenticated() ? <PortfolioDetails /> : <Navigate to="/" />}
                />
                <Route
                    path="/portfolio/:id/report/form"
                    element={isAuthenticated() ? <PortfolioReportForm /> : <Navigate to="/" />}
                />
                <Route
                    path="/portfolio/:id/report/view"
                    element={isAuthenticated() ? <PortfolioReportView /> : <Navigate to="/" />}
                />

                {/* NOWA TRASA dla wykresu portfela */}
                <Route
                    path="/portfolio/:id/chart"
                    element={isAuthenticated() ? <PortfolioChart /> : <Navigate to="/" />}
                />

                <Route path="*" element={<Navigate to="/" />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;
