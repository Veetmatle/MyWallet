import { useState } from "react";
import { Link } from "react-router-dom";
import "../auth.css";

export default function Login() {
    const [usernameOrEmail, setUsernameOrEmail] = useState("");
    const [password, setPassword] = useState("");

    const [errors, setErrors] = useState<{ [key: string]: string }>({});

    const validate = () => {
        const newErrors: { [key: string]: string } = {};

        if (!usernameOrEmail.trim()) {
            newErrors.usernameOrEmail = "Login lub e-mail jest wymagany.";
        }

        if (!password) {
            newErrors.password = "Hasło jest wymagane.";
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!validate()) return;

        try {
            const response = await fetch("/api/user/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ usernameOrEmail, password }),
            });

            if (response.status === 400) {
                const data = await response.json();
                const backendErrors: { [key: string]: string } = {};
                for (const key in data.errors) {
                    backendErrors[key.toLowerCase()] = data.errors[key][0];
                }
                setErrors(backendErrors);
                return;
            }

            if (response.status === 401) {
                setErrors({ global: "Nieprawidłowe dane logowania." });
                return;
            }

            if (!response.ok) throw new Error("Logowanie nie powiodło się.");

            const user = await response.json();
            localStorage.setItem("user", JSON.stringify(user));
            window.location.href = "/dashboard";
        } catch (err: any) {
            setErrors({ global: err.message });
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
                        />
                        {errors.usernameOrEmail && <div className="error-message">{errors.usernameOrEmail}</div>}
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
                        />
                        {errors.password && <div className="error-message">{errors.password}</div>}
                    </div>
                    <button type="submit" className="btn btn-primary">
                        Zaloguj się
                    </button>
                    {errors.global && <div className="error-message">{errors.global}</div>}
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
