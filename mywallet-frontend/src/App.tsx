import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Login from "./pages/Login";
import Register from "./pages/Register";
import Dashboard from "./pages/Dashboard";

// Import stylów
import "./App.css";
import "./auth.css";
import PortfolioDetails from "./pages/PortfolioDetails";

function App() {
    // Sprawdzenie, czy użytkownik jest zalogowany
    const isAuthenticated = () => {
        return localStorage.getItem("user") !== null;
    };

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
                    path="/portfolio/:id"
                    element={isAuthenticated() ? <PortfolioDetails /> : <Navigate to="/" />}
                />
                <Route
                    path="/dashboard"
                    element={isAuthenticated() ? <Dashboard /> : <Navigate to="/" />}
                />
                {/* Przekierowanie nieznanych ścieżek do strony głównej */}
                <Route path="*" element={<Navigate to="/" />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;