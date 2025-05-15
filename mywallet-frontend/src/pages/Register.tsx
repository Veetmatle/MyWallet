import { useState } from "react";
import { Link } from "react-router-dom";
import { register } from "../api/user";
import "../auth.css";

export default function Register() {
    const [username, setUsername] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [success, setSuccess] = useState("");
    const [error, setError] = useState("");

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const result = await register(username, email, password);
            setSuccess(result);
            setError("");
            // Opcjonalnie: automatyczne przekierowanie po rejestracji
            // setTimeout(() => {
            //     window.location.href = "/";
            // }, 3000);
        } catch (err: any) {
            setError(err.message);
            setSuccess("");
        }
    };

    return (
        <div className="auth-container">
            <div className="auth-card">
                <img src="/logo192.png" alt="MyWallet Logo" className="auth-logo" />
                <h2 className="auth-title">Rejestracja w MyWallet</h2>
                <form onSubmit={handleRegister} className="auth-form">
                    <div className="form-group">
                        <label htmlFor="username">Login</label>
                        <input
                            id="username"
                            type="text"
                            placeholder="Wybierz login"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            className="form-control"
                            required
                        />
                    </div>
                    <div className="form-group">
                        <label htmlFor="email">E-mail</label>
                        <input
                            id="email"
                            type="email"
                            placeholder="Wprowadź adres e-mail"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            className="form-control"
                            required
                        />
                    </div>
                    <div className="form-group">
                        <label htmlFor="password">Hasło</label>
                        <input
                            id="password"
                            type="password"
                            placeholder="Wybierz bezpieczne hasło"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="form-control"
                            required
                        />
                    </div>
                    <button type="submit" className="btn btn-primary">
                        Zarejestruj się
                    </button>
                    {success && <div className="success-message">{success}</div>}
                    {error && <div className="error-message">{error}</div>}
                </form>
                <div className="auth-footer">
                    Masz już konto?
                    <Link to="/" className="auth-link">
                        Zaloguj się!
                    </Link>
                </div>
            </div>
        </div>
    );
}