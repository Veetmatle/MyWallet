import { useState } from "react";
import { Link } from "react-router-dom";
import { login } from "../api/user";
import "../auth.css";

export default function Login() {
    const [usernameOrEmail, setUsernameOrEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const user = await login(usernameOrEmail, password);
            console.log("Zalogowano jako:", user);
            localStorage.setItem("user", JSON.stringify(user));
            window.location.href = "/dashboard";
        } catch (err: any) {
            setError(err.message);
        }
    };

    return (
        <div className="auth-container">
            <div className="auth-card">
                <img src="/logo192.png" alt="MyWallet Logo" className="auth-logo" />
                <h2 className="auth-title">Logowanie do MyWallet</h2>
                <form onSubmit={handleLogin} className="auth-form">
                    <div className="form-group">
                        <label htmlFor="usernameOrEmail">Email lub login</label>
                        <input
                            id="usernameOrEmail"
                            type="text"
                            placeholder="Wprowadź email lub login"
                            value={usernameOrEmail}
                            onChange={(e) => setUsernameOrEmail(e.target.value)}
                            className="form-control"
                            required
                        />
                    </div>
                    <div className="form-group">
                        <label htmlFor="password">Hasło</label>
                        <input
                            id="password"
                            type="password"
                            placeholder="Wprowadź hasło"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="form-control"
                            required
                        />
                    </div>
                    <button type="submit" className="btn btn-primary">
                        Zaloguj się
                    </button>
                    {error && <div className="error-message">{error}</div>}
                </form>
                <div className="auth-footer">
                    Nie masz jeszcze konta?
                    <Link to="/register" className="auth-link">
                        Zarejestruj się!
                    </Link>
                </div>
            </div>
        </div>
    );
}